use reqwest::{header, redirect::Policy, Client, StatusCode};
use serde::{Deserialize, Serialize};
use serde_json::Value;
use sha2::{Digest, Sha256};
use std::{
    fs::{self, File},
    io::ErrorKind,
    io::Read,
    path::PathBuf,
    sync::Arc,
    time::{Duration, SystemTime, UNIX_EPOCH},
};
use tauri::async_runtime::{channel, Receiver, Sender, Mutex};

const USAGE_URL: &str = "https://chatgpt.com/backend-api/wham/usage";
const RESET_CREDITS_URL: &str = "https://chatgpt.com/backend-api/wham/rate-limit-reset-credits";
const MAX_AUTH_BYTES: u64 = 256 * 1024;
const MAX_RESPONSE_BYTES: usize = 1024 * 1024;
const CACHE_TTL: Duration = Duration::from_secs(60);

#[derive(Clone, Copy, Serialize, PartialEq, Eq, Debug)]
#[serde(rename_all = "camelCase")]
pub enum QuotaStatus {
    Available,
    Partial,
    Stale,
    Unavailable,
    SignedOut,
    RateLimited,
}

#[derive(Clone, Serialize, PartialEq, Debug)]
#[serde(rename_all = "camelCase")]
pub struct QuotaWindow {
    pub remaining_percent: u8,
    pub window_seconds: u64,
    pub resets_at_unix_seconds: Option<i64>,
}

#[derive(Clone, Copy, Serialize, PartialEq, Eq, Debug)]
#[serde(rename_all = "camelCase")]
pub enum ResetCreditStatus {
    Available,
    Partial,
    Stale,
    Unavailable,
}

#[derive(Clone, Serialize, PartialEq, Eq, Debug)]
#[serde(rename_all = "camelCase")]
pub struct ResetCreditData {
    pub status: ResetCreditStatus,
    pub available_count: Option<u32>,
    pub expires_at_unix_seconds: Option<i64>,
}

impl ResetCreditData {
    fn unavailable() -> Self {
        Self { status: ResetCreditStatus::Unavailable, available_count: None, expires_at_unix_seconds: None }
    }

    fn mark_stale(&mut self) {
        if matches!(self.status, ResetCreditStatus::Available | ResetCreditStatus::Partial) {
            self.status = ResetCreditStatus::Stale;
        }
    }
}

#[derive(Clone, Serialize, PartialEq, Debug)]
#[serde(rename_all = "camelCase")]
pub struct QuotaData {
    pub status: QuotaStatus,
    pub plan_label: Option<String>,
    pub windows: Vec<QuotaWindow>,
    pub fetched_at_unix_seconds: Option<i64>,
    pub retry_after_seconds: Option<u64>,
    pub reset_credits: ResetCreditData,
}

impl QuotaData {
    fn unavailable(status: QuotaStatus) -> Self {
        Self {
            status,
            plan_label: None,
            windows: Vec::new(),
            fetched_at_unix_seconds: None,
            retry_after_seconds: None,
            reset_credits: ResetCreditData::unavailable(),
        }
    }
}

pub fn parse_usage(usage: &Value) -> QuotaData {
    let plan_label = usage.get("plan_type").and_then(Value::as_str)
        .filter(|value| !value.is_empty() && value.len() <= 40 && value.bytes().all(|byte| byte.is_ascii_alphanumeric() || b"-_".contains(&byte)))
        .map(str::to_owned);
    let Some(rate_limit) = usage.get("rate_limit").and_then(Value::as_object) else {
        return QuotaData {
            status: QuotaStatus::Unavailable,
            plan_label,
            windows: Vec::new(),
            fetched_at_unix_seconds: None,
            retry_after_seconds: None,
            reset_credits: ResetCreditData::unavailable(),
        };
    };
    let mut windows = Vec::with_capacity(2);
    let mut partial = false;
    for key in ["primary_window", "secondary_window"] {
        let Some(value) = rate_limit.get(key) else { continue };
        let parsed = value.as_object().and_then(|window| {
            let remaining = window.get("remaining_percent").and_then(Value::as_f64)
                .or_else(|| window.get("used_percent").and_then(Value::as_f64).map(|used| 100.0 - used))?;
            let duration = window.get("limit_window_seconds").and_then(Value::as_u64)?;
            if !remaining.is_finite() || !(0.0..=100.0).contains(&remaining) || duration == 0 || duration > 366 * 24 * 60 * 60 { return None; }
            Some(QuotaWindow {
                remaining_percent: remaining.round() as u8,
                window_seconds: duration,
                resets_at_unix_seconds: window.get("reset_at").and_then(Value::as_i64),
            })
        });
        match parsed { Some(window) => windows.push(window), None => partial = true }
    }
    let status = if windows.is_empty() { QuotaStatus::Unavailable } else if partial { QuotaStatus::Partial } else { QuotaStatus::Available };
    QuotaData {
        status,
        plan_label,
        windows,
        fetched_at_unix_seconds: None,
        retry_after_seconds: None,
        reset_credits: ResetCreditData::unavailable(),
    }
}

fn parse_credit_expiry(value: &Value) -> Option<i64> {
    let timestamp = value.as_i64().or_else(|| {
        value.as_str()
            .and_then(|value| chrono::DateTime::parse_from_rfc3339(value).ok())
            .map(|value| value.timestamp())
    })?;
    (timestamp > 0 && timestamp <= 253_402_300_799).then_some(timestamp)
}

fn parse_reset_credits(value: &Value, now_unix_seconds: i64) -> ResetCreditData {
    let Some(available_count) = value.get("available_count")
        .and_then(Value::as_u64)
        .filter(|count| *count <= i32::MAX as u64)
        .map(|count| count as u32) else {
            return ResetCreditData::unavailable();
        };
    let expires_at_unix_seconds = value.get("credits")
        .and_then(Value::as_array)
        .into_iter()
        .flatten()
        .filter_map(|credit| credit.get("expires_at"))
        .filter_map(parse_credit_expiry)
        .filter(|expiry| *expiry > now_unix_seconds)
        .min();
    let status = if available_count == 0 || expires_at_unix_seconds.is_some() {
        ResetCreditStatus::Available
    } else {
        ResetCreditStatus::Partial
    };
    ResetCreditData { status, available_count: Some(available_count), expires_at_unix_seconds }
}

#[derive(Clone)]
pub struct QuotaReader {
    client: Client,
    state: Arc<Mutex<State>>,
}

#[derive(Default)]
struct State {
    next_request_id: u64,
    cache: Option<Cached>,
    flight: Option<Flight>,
}

#[derive(Clone)]
struct Cached {
    identity: [u8; 32],
    stored_at: SystemTime,
    data: QuotaData,
}

struct Flight {
    request_id: u64,
    identity: [u8; 32],
    waiters: Vec<Sender<QuotaData>>,
}

struct Auth {
    token: String,
    account_id: Option<String>,
    identity: [u8; 32],
}

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
enum AuthRecheck {
    Same,
    Changed,
    Unknown,
}

fn recheck_auth(current: Result<Option<Auth>, ()>, identity: [u8; 32]) -> AuthRecheck {
    match current {
        Ok(Some(current)) if current.identity == identity => AuthRecheck::Same,
        Ok(Some(_)) | Ok(None) => AuthRecheck::Changed,
        Err(()) => AuthRecheck::Unknown,
    }
}

fn parse_retry_after(value: &str, now_unix_seconds: Option<i64>) -> Option<u64> {
    let value = value.trim();
    if let Ok(seconds) = value.parse::<u64>() { return Some(seconds); }
    let now = now_unix_seconds?;
    let date = chrono::DateTime::parse_from_rfc2822(value).ok()?;
    if date.offset().local_minus_utc() != 0 { return None; }
    Some(date.timestamp().saturating_sub(now).max(0) as u64)
}

impl QuotaReader {
    pub fn new() -> Result<Self, reqwest::Error> {
        let client = Client::builder()
            .https_only(true)
            .redirect(Policy::none())
            .timeout(Duration::from_secs(12))
            .build()?;
        Ok(Self { client, state: Arc::new(Mutex::new(State::default())) })
    }

    pub async fn read(&self, force_refresh: bool) -> QuotaData {
        let mut auth = match tauri::async_runtime::spawn_blocking(read_auth).await {
            Ok(Ok(Some(auth))) => auth,
            Ok(Ok(None)) => {
                let mut state = self.state.lock().await;
                state.cache = None;
                return QuotaData::unavailable(QuotaStatus::SignedOut);
            }
            _ => {
                let mut state = self.state.lock().await;
                state.cache = None;
                return QuotaData::unavailable(QuotaStatus::Unavailable);
            }
        };

        let cached_data = {
            let mut state = self.state.lock().await;
            if state.cache.as_ref().is_some_and(|cached| cached.identity != auth.identity) { state.cache = None; }
            if force_refresh { None } else {
                state.cache.as_ref().filter(|cached| cache_is_fresh(cached, auth.identity)).map(|cached| cached.data.clone())
            }
        };
        if let Some(cached_data) = cached_data {
            match tauri::async_runtime::spawn_blocking(read_auth).await {
                Ok(Ok(Some(current))) if current.identity == auth.identity => return cached_data,
                Ok(Ok(Some(current))) => auth = current,
                Ok(Ok(None)) => {
                    self.state.lock().await.cache = None;
                    return QuotaData::unavailable(QuotaStatus::SignedOut);
                }
                _ => {
                    self.state.lock().await.cache = None;
                    return QuotaData::unavailable(QuotaStatus::Unavailable);
                }
            }
        }

        let (sender, receiver) = channel(1);
        let (request_to_start, retired_waiters) = {
            let mut state = self.state.lock().await;
            if state.cache.as_ref().is_some_and(|cached| cached.identity != auth.identity) { state.cache = None; }
            if let Some(flight) = state.flight.as_mut().filter(|flight| flight.identity == auth.identity) {
                flight.waiters.push(sender);
                (None, Vec::new())
            } else {
                let retired_waiters = state.flight.take().map(|flight| flight.waiters).unwrap_or_default();
                state.next_request_id = state.next_request_id.wrapping_add(1).max(1);
                let request_id = state.next_request_id;
                state.flight = Some(Flight { request_id, identity: auth.identity, waiters: vec![sender] });
                (Some((request_id, auth)), retired_waiters)
            }
        };

        for waiter in retired_waiters { let _ = waiter.send(QuotaData::unavailable(QuotaStatus::Unavailable)).await; }
        if let Some((request_id, auth)) = request_to_start {
            let reader = self.clone();
            tauri::async_runtime::spawn(async move { reader.fetch_and_publish(request_id, auth).await; });
        }
        receive(receiver).await
    }

    async fn fetch_and_publish(&self, request_id: u64, auth: Auth) {
        let mut result = self.fetch(&auth).await;
        let current_auth = tauri::async_runtime::spawn_blocking(read_auth).await;
        let identity_check = match current_auth {
            Ok(current) => recheck_auth(current, auth.identity),
            Err(_) => AuthRecheck::Unknown,
        };
        if identity_check != AuthRecheck::Same {
            result = QuotaData::unavailable(match identity_check {
                AuthRecheck::Changed => QuotaStatus::SignedOut,
                AuthRecheck::Unknown => QuotaStatus::Unavailable,
                AuthRecheck::Same => unreachable!(),
            });
        }

        let waiters = {
            let mut state = self.state.lock().await;
            let flight_matches = state.flight.as_ref().is_some_and(|flight| flight.request_id == request_id);
            if (identity_check == AuthRecheck::Changed
                || (identity_check == AuthRecheck::Same && result.status == QuotaStatus::SignedOut))
                && state.cache.as_ref().is_some_and(|cached| cached.identity == auth.identity)
            { state.cache = None; }
            if !flight_matches { Vec::new() } else {
                let flight = state.flight.take().expect("flight checked above");
                if identity_check == AuthRecheck::Same && matches!(result.status, QuotaStatus::Unavailable | QuotaStatus::RateLimited | QuotaStatus::Stale) {
                    if let Some(cached) = state.cache.as_mut().filter(|cached| cached.identity == auth.identity) {
                        cached.data.status = QuotaStatus::Stale;
                        cached.data.retry_after_seconds = result.retry_after_seconds;
                        cached.data.reset_credits.mark_stale();
                    }
                }
                if identity_check == AuthRecheck::Same && matches!(result.status, QuotaStatus::Available | QuotaStatus::Partial) {
                    state.cache = Some(Cached { identity: auth.identity, stored_at: SystemTime::now(), data: result.clone() });
                }
                result = if identity_check == AuthRecheck::Same && matches!(result.status, QuotaStatus::Unavailable | QuotaStatus::RateLimited) {
                    if let Some(cached) = state.cache.as_ref().filter(|cached| cached.identity == auth.identity) {
                        let mut stale = cached.data.clone();
                        stale.status = QuotaStatus::Stale;
                        stale.retry_after_seconds = result.retry_after_seconds;
                        stale.reset_credits.mark_stale();
                        stale
                    } else { result }
                } else { result };
                flight.waiters
            }
        };
        for waiter in waiters { let _ = waiter.send(result.clone()).await; }
    }

    async fn fetch(&self, auth: &Auth) -> QuotaData {
        let mut request = self.client.get(USAGE_URL).bearer_auth(&auth.token);
        if let Some(account_id) = auth.account_id.as_deref() { request = request.header("ChatGPT-Account-Id", account_id); }
        let response = match request.send().await { Ok(response) => response, Err(_) => return self.stale_or_unavailable(auth.identity).await };
        let status = response.status();
        if status == StatusCode::UNAUTHORIZED || status == StatusCode::FORBIDDEN { return QuotaData::unavailable(QuotaStatus::SignedOut); }
        if status == StatusCode::TOO_MANY_REQUESTS {
            let retry_after_seconds = response.headers().get(header::RETRY_AFTER)
                .and_then(|value| value.to_str().ok())
                .and_then(|value| parse_retry_after(value, unix_now()));
            return QuotaData { retry_after_seconds, ..QuotaData::unavailable(QuotaStatus::RateLimited) };
        }
        if !status.is_success() { return self.stale_or_unavailable(auth.identity).await; }
        if response.content_length().is_some_and(|length| length > MAX_RESPONSE_BYTES as u64) {
            return self.stale_or_unavailable(auth.identity).await;
        }

        let mut body = Vec::new();
        let mut response = response;
        loop {
            match response.chunk().await {
                Ok(Some(chunk)) if body.len().saturating_add(chunk.len()) <= MAX_RESPONSE_BYTES => body.extend_from_slice(&chunk),
                Ok(Some(_)) | Err(_) => return self.stale_or_unavailable(auth.identity).await,
                Ok(None) => break,
            }
        }
        let Ok(value) = serde_json::from_slice::<Value>(&body) else { return self.stale_or_unavailable(auth.identity).await };
        let mut data = parse_usage(&value);
        if matches!(data.status, QuotaStatus::Available | QuotaStatus::Partial) {
            data.fetched_at_unix_seconds = unix_now();
            data.reset_credits = self.fetch_reset_credits(auth).await;
            data
        } else { self.stale_or_unavailable(auth.identity).await }
    }

    async fn fetch_reset_credits(&self, auth: &Auth) -> ResetCreditData {
        let mut request = self.client.get(RESET_CREDITS_URL).bearer_auth(&auth.token);
        if let Some(account_id) = auth.account_id.as_deref() { request = request.header("ChatGPT-Account-Id", account_id); }
        let response = match request.send().await {
            Ok(response) if response.status().is_success() => response,
            _ => return ResetCreditData::unavailable(),
        };
        if response.content_length().is_some_and(|length| length > MAX_RESPONSE_BYTES as u64) {
            return ResetCreditData::unavailable();
        }
        let mut body = Vec::new();
        let mut response = response;
        loop {
            match response.chunk().await {
                Ok(Some(chunk)) if body.len().saturating_add(chunk.len()) <= MAX_RESPONSE_BYTES => body.extend_from_slice(&chunk),
                Ok(Some(_)) | Err(_) => return ResetCreditData::unavailable(),
                Ok(None) => break,
            }
        }
        let Ok(value) = serde_json::from_slice::<Value>(&body) else { return ResetCreditData::unavailable() };
        parse_reset_credits(&value, unix_now().unwrap_or(i64::MAX))
    }

    async fn stale_or_unavailable(&self, identity: [u8; 32]) -> QuotaData {
        let state = self.state.lock().await;
        if let Some(cached) = state.cache.as_ref().filter(|cached| cached.identity == identity) {
            let mut stale = cached.data.clone();
            stale.status = QuotaStatus::Stale;
            stale.reset_credits.mark_stale();
            stale
        } else { QuotaData::unavailable(QuotaStatus::Unavailable) }
    }
}

async fn receive(mut receiver: Receiver<QuotaData>) -> QuotaData {
    receiver.recv().await.unwrap_or_else(|| QuotaData::unavailable(QuotaStatus::Unavailable))
}

fn read_auth() -> Result<Option<Auth>, ()> {
    let Some(home) = codex_home() else { return Err(()) };
    let path = home.join("auth.json");
    let metadata = match fs::symlink_metadata(&path) {
        Ok(metadata) => metadata,
        Err(error) if error.kind() == ErrorKind::NotFound => return Ok(None),
        Err(_) => return Err(()),
    };
    if is_reparse_point(&metadata) || !metadata.is_file() || metadata.len() == 0 || metadata.len() > MAX_AUTH_BYTES { return Err(()); }
    let file = File::open(&path).map_err(|_| ())?;
    let length = file.metadata().map_err(|_| ())?.len();
    if length == 0 || length > MAX_AUTH_BYTES { return Err(()); }
    let mut bytes = Vec::with_capacity(length as usize);
    file.take(MAX_AUTH_BYTES + 1).read_to_end(&mut bytes).map_err(|_| ())?;
    if bytes.len() as u64 > MAX_AUTH_BYTES { return Err(()); }
    let value: Value = serde_json::from_slice(&bytes).map_err(|_| ())?;
    let tokens = value.get("tokens").and_then(Value::as_object);
    let Some(token) = tokens.and_then(|value| value.get("access_token"))
        .or_else(|| value.get("access_token")).and_then(Value::as_str).map(str::trim) else { return Ok(None) };
    if token.is_empty() { return Ok(None); }
    if token.len() > 64 * 1024 { return Err(()); }
    let account_id = tokens.and_then(|value| value.get("account_id"))
        .or_else(|| value.get("account_id")).and_then(Value::as_str)
        .filter(|value| !value.is_empty() && value.len() <= 512).map(str::to_owned);
    let identity_source = account_id.as_deref().unwrap_or(token);
    let identity = Sha256::digest(identity_source.as_bytes()).into();
    Ok(Some(Auth { token: token.to_owned(), account_id, identity }))
}

fn cache_is_fresh(cached: &Cached, identity: [u8; 32]) -> bool {
    cached.identity == identity
        && cached.data.status != QuotaStatus::Stale
        && cached.stored_at.elapsed().is_ok_and(|age| age < CACHE_TTL)
}

fn codex_home() -> Option<PathBuf> {
    if let Some(path) = std::env::var_os("CODEX_HOME").filter(|value| !value.is_empty()) { return Some(PathBuf::from(path)); }
    std::env::var_os("USERPROFILE").filter(|value| !value.is_empty()).map(|path| PathBuf::from(path).join(".codex"))
}

fn unix_now() -> Option<i64> {
    SystemTime::now().duration_since(UNIX_EPOCH).ok().map(|duration| duration.as_secs().min(i64::MAX as u64) as i64)
}

#[cfg(windows)]
fn is_reparse_point(metadata: &fs::Metadata) -> bool {
    use std::os::windows::fs::MetadataExt;
    metadata.file_attributes() & 0x400 != 0
}

#[cfg(not(windows))]
fn is_reparse_point(metadata: &fs::Metadata) -> bool { metadata.file_type().is_symlink() }

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn preserves_plan_and_real_window_duration_without_inventing_five_hours() {
        let usage = serde_json::json!({"plan_type":"prolite","rate_limit":{"primary_window":{"remaining_percent":98,"limit_window_seconds":604800,"reset_at":1792649595}}});
        let data = parse_usage(&usage);
        assert_eq!(data.status, QuotaStatus::Available);
        assert_eq!(data.plan_label.as_deref(), Some("prolite"));
        assert_eq!(data.windows[0].window_seconds, 604800);
        assert_eq!(data.windows[0].remaining_percent, 98);
    }

    #[test]
    fn malformed_or_unknown_windows_do_not_create_quota_readings() {
        let usage = serde_json::json!({"plan_type":"mystery-tier","rate_limit":{"primary_window":{"used_percent":22}}});
        let data = parse_usage(&usage);
        assert_eq!(data.status, QuotaStatus::Unavailable);
        assert_eq!(data.plan_label.as_deref(), Some("mystery-tier"));
        assert!(data.windows.is_empty());
    }

    #[test]
    fn converts_used_percent_only_when_a_real_window_duration_is_present() {
        let usage = serde_json::json!({"rate_limit":{"primary_window":{"used_percent":28,"limit_window_seconds":18000}}});
        let data = parse_usage(&usage);
        assert_eq!(data.status, QuotaStatus::Available);
        assert_eq!(data.windows[0].remaining_percent, 72);
        assert_eq!(data.windows[0].window_seconds, 18000);
    }

    #[test]
    fn retry_after_preserves_long_delta_and_http_date_floors() {
        assert_eq!(parse_retry_after("90000", Some(1_800_000_000)), Some(90_000));
        let date = "Sun, 06 Nov 1994 08:49:37 GMT";
        let retry_at = chrono::DateTime::parse_from_rfc2822(date).unwrap().timestamp();
        assert_eq!(parse_retry_after(date, Some(retry_at - 300)), Some(300));
        assert_eq!(parse_retry_after(date, Some(retry_at + 1)), Some(0));
        assert_eq!(parse_retry_after("not-a-date", Some(retry_at)), None);
    }

    #[test]
    fn auth_recheck_distinguishes_read_error_from_a_confirmed_identity_change() {
        let identity = [7; 32];
        let auth = |identity| Auth { token: "token".to_owned(), account_id: None, identity };
        assert_eq!(recheck_auth(Ok(Some(auth(identity))), identity), AuthRecheck::Same);
        assert_eq!(recheck_auth(Ok(Some(auth([8; 32]))), identity), AuthRecheck::Changed);
        assert_eq!(recheck_auth(Ok(None), identity), AuthRecheck::Changed);
        assert_eq!(recheck_auth(Err(()), identity), AuthRecheck::Unknown);
    }

    #[test]
    fn reset_credit_count_requires_the_exact_integer_field_and_preserves_zero() {
        let zero = parse_reset_credits(&serde_json::json!({"available_count":0,"credits":[]}), 1_800_000_000);
        assert_eq!(zero.status, ResetCreditStatus::Available);
        assert_eq!(zero.available_count, Some(0));
        assert_eq!(zero.expires_at_unix_seconds, None);

        for payload in [
            serde_json::json!({}),
            serde_json::json!({"count":2}),
            serde_json::json!({"available_count":2.5}),
            serde_json::json!({"available_count":-1}),
            serde_json::json!({"available_count":"2"}),
            serde_json::json!({"available_count":2147483648_u64}),
        ] {
            let parsed = parse_reset_credits(&payload, 1_800_000_000);
            assert_eq!(parsed.status, ResetCreditStatus::Unavailable);
            assert_eq!(parsed.available_count, None);
        }
    }

    #[test]
    fn reset_credit_expiry_uses_only_valid_future_timestamps() {
        let now = 1_800_000_000_i64;
        let payload = serde_json::json!({
            "available_count": 2,
            "credits": [
                {"expires_at": now + 3600},
                {"expires_at": "2030-01-01T00:00:00Z"},
                {"expires_at": now - 1},
                {"expires_at": "not-a-timestamp"}
            ]
        });
        let parsed = parse_reset_credits(&payload, now);
        assert_eq!(parsed.status, ResetCreditStatus::Available);
        assert_eq!(parsed.available_count, Some(2));
        assert_eq!(parsed.expires_at_unix_seconds, Some(now + 3600));

        let malformed_expiry = parse_reset_credits(
            &serde_json::json!({"available_count":2,"credits":[{"expires_at":"bad"}]}),
            now,
        );
        assert_eq!(malformed_expiry.status, ResetCreditStatus::Partial);
        assert_eq!(malformed_expiry.available_count, Some(2));
        assert_eq!(malformed_expiry.expires_at_unix_seconds, None);
    }

    #[test]
    fn stale_values_are_not_treated_as_fresh_for_the_next_automatic_poll() {
        let identity = [7; 32];
        let cached = Cached {
            identity,
            stored_at: SystemTime::now(),
            data: QuotaData {
                status: QuotaStatus::Stale,
                plan_label: None,
                windows: Vec::new(),
                fetched_at_unix_seconds: None,
                retry_after_seconds: None,
                reset_credits: ResetCreditData::unavailable(),
            },
        };
        assert!(!cache_is_fresh(&cached, identity));
    }
}
