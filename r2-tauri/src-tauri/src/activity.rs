use chrono::DateTime;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::{
    collections::HashMap,
    fs::{self, File},
    io::{Read, Seek, SeekFrom},
    path::{Path, PathBuf},
    time::{Duration, Instant, SystemTime, UNIX_EPOCH},
};

const MAX_SCAN_FILES: usize = 64;
const MAX_DISCOVERY_ENTRIES: usize = 20_000;
const MAX_DISCOVERY_DIRS: usize = 4_096;
const MAX_PATH_DEPTH: usize = 32;
const MAX_SCAN_BYTES: u64 = 8 * 1024 * 1024;
const MAX_FILE_BYTES: u64 = 1024 * 1024;
const MAX_LINE_BYTES: usize = 256 * 1024;
const MAX_JSON_DEPTH: usize = 32;
const MAX_SCAN_TIME: Duration = Duration::from_secs(2);
const BUFFER_BYTES: usize = 32 * 1024;

#[derive(Clone, Copy, Serialize, PartialEq, Eq, Debug)]
#[serde(rename_all = "camelCase")]
pub enum ActivityStatus {
    Available,
    Partial,
    Unavailable,
}

#[derive(Clone, Serialize, PartialEq, Debug)]
#[serde(rename_all = "camelCase")]
pub struct ActivityData {
    pub status: ActivityStatus,
    pub total_tokens: Option<u64>,
    pub buckets: Vec<Option<u64>>,
    pub scanned_at_unix_seconds: Option<i64>,
}

#[derive(Deserialize)]
struct Record {
    #[serde(rename = "type")]
    record_type: Option<String>,
    timestamp: Option<String>,
    payload: Option<Payload>,
}

#[derive(Deserialize)]
struct Payload {
    #[serde(rename = "type")]
    payload_type: Option<String>,
    id: Option<String>,
    #[serde(
        alias = "forked_from",
        alias = "forkedFrom",
        alias = "parent_session_id",
        alias = "parentSessionId"
    )]
    parent_id: Option<String>,
    info: Option<Info>,
}

#[derive(Deserialize)]
struct Info {
    total_token_usage: Option<TokenUsage>,
}

#[derive(Deserialize)]
struct TokenUsage {
    total_tokens: Option<u64>,
}

struct Candidate {
    path: PathBuf,
    modified: SystemTime,
    length: u64,
}

#[derive(Default)]
struct FileScan {
    session_id_hash: Option<[u8; 32]>,
    forked: bool,
    partial: bool,
    segments: Vec<Vec<(i64, u64)>>,
}

fn unavailable(scanned_at_unix_seconds: Option<i64>) -> ActivityData {
    ActivityData {
        status: ActivityStatus::Unavailable,
        total_tokens: None,
        buckets: vec![None; 12],
        scanned_at_unix_seconds,
    }
}

pub fn valid_day_edges(edges: &[i64]) -> bool {
    if edges.len() != 13 || edges.windows(2).any(|pair| pair[1] <= pair[0]) {
        return false;
    }
    let Some(duration) = edges.last().and_then(|end| end.checked_sub(edges[0])) else {
        return false;
    };
    (23 * 60 * 60..=25 * 60 * 60).contains(&duration)
}

pub fn aggregate_segments(edges: &[i64], segments: &[Vec<(i64, u64)>]) -> ActivityData {
    if !valid_day_edges(edges) {
        return unavailable(None);
    }

    let mut buckets = [0u64; 12];
    let mut bucket_has_delta = [false; 12];
    let mut total = 0u64;
    let mut has_delta = false;

    for segment in segments {
        let mut previous: Option<(i64, u64)> = None;
        for &(timestamp, value) in segment {
            let Some((previous_time, previous_value)) = previous else {
                previous = Some((timestamp, value));
                continue;
            };

            if timestamp < previous_time || value < previous_value {
                previous = Some((timestamp, value));
                continue;
            }

            let delta = value - previous_value;
            if delta > 0 {
                let in_day = previous_time >= edges[0] && timestamp <= edges[12];
                let bucket = if in_day {
                    (0..12).find(|&index| previous_time >= edges[index] && timestamp <= edges[index + 1] && previous_time < edges[index + 1])
                } else {
                    None
                };

                // Missing baselines and cross-bucket intervals add no count; observed deltas remain a partial lower bound.
                if let Some(index) = bucket {
                    if let (Some(bucket_total), Some(day_total)) = (buckets[index].checked_add(delta), total.checked_add(delta)) {
                        buckets[index] = bucket_total;
                        total = day_total;
                        bucket_has_delta[index] = true;
                        has_delta = true;
                    }
                }
            }
            previous = Some((timestamp, value));
        }
    }

    if !has_delta {
        return unavailable(None);
    }

    ActivityData {
        status: ActivityStatus::Partial,
        total_tokens: Some(total),
        buckets: buckets
            .into_iter()
            .zip(bucket_has_delta)
            .map(|(value, has_delta)| has_delta.then_some(value))
            .collect(),
        scanned_at_unix_seconds: None,
    }
}

pub fn scan_local_activity(edges: &[i64]) -> ActivityData {
    let now = unix_now();
    if !valid_day_edges(edges) {
        return unavailable(now);
    }

    let Some(home) = codex_home() else {
        return unavailable(now);
    };
    let sessions = home.join("sessions");
    let Ok(root_metadata) = fs::symlink_metadata(&sessions) else {
        return unavailable(now);
    };
    if is_reparse_point(&root_metadata) || !root_metadata.is_dir() {
        return unavailable(now);
    }

    let started = Instant::now();
    let deadline = started + MAX_SCAN_TIME;
    let (candidates, mut partial) = discover_candidates(&sessions, deadline);
    let mut remaining_bytes = MAX_SCAN_BYTES;
    let mut scans = Vec::with_capacity(candidates.len());

    for candidate in candidates {
        if Instant::now() >= deadline || remaining_bytes == 0 {
            partial = true;
            break;
        }
        match scan_file(&candidate, &mut remaining_bytes, deadline) {
            Ok(scan) => {
                partial |= scan.partial;
                scans.push(scan);
            }
            Err(()) => partial = true,
        }
    }

    let mut session_counts = HashMap::<[u8; 32], usize>::new();
    for scan in &scans {
        if let Some(session_id) = scan.session_id_hash {
            *session_counts.entry(session_id).or_default() += 1;
        } else {
            partial = true;
        }
        if scan.forked {
            partial = true;
        }
    }

    let mut segments = Vec::new();
    for scan in scans {
        let Some(session_id) = scan.session_id_hash else { continue };
        if scan.forked || session_counts.get(&session_id).copied().unwrap_or(0) != 1 {
            partial = true;
            continue;
        }
        segments.extend(scan.segments);
    }

    let mut result = aggregate_segments(edges, &segments);
    result.scanned_at_unix_seconds = now;
    if result.total_tokens.is_some() {
        // The capped recent-file scan is a lower bound even when no read error occurred.
        result.status = ActivityStatus::Partial;
    } else if partial {
        result.status = ActivityStatus::Unavailable;
    }
    result
}

fn codex_home() -> Option<PathBuf> {
    if let Some(path) = std::env::var_os("CODEX_HOME").filter(|value| !value.is_empty()) {
        return Some(PathBuf::from(path));
    }
    std::env::var_os("USERPROFILE").filter(|value| !value.is_empty()).map(|path| PathBuf::from(path).join(".codex"))
}

fn discover_candidates(root: &Path, deadline: Instant) -> (Vec<Candidate>, bool) {
    let mut stack = vec![(root.to_path_buf(), 0usize)];
    let mut candidates = Vec::<Candidate>::with_capacity(MAX_SCAN_FILES);
    let mut visited_entries = 0usize;
    let mut visited_directories = 0usize;
    let mut partial = false;

    while let Some((directory, depth)) = stack.pop() {
        if Instant::now() >= deadline || visited_directories >= MAX_DISCOVERY_DIRS {
            partial = true;
            break;
        }
        if depth > MAX_PATH_DEPTH {
            partial = true;
            continue;
        }
        let Ok(metadata) = fs::symlink_metadata(&directory) else {
            partial = true;
            continue;
        };
        if is_reparse_point(&metadata) || !metadata.is_dir() {
            partial = true;
            continue;
        }
        visited_directories += 1;

        let Ok(entries) = fs::read_dir(&directory) else {
            partial = true;
            continue;
        };
        for entry in entries {
            if Instant::now() >= deadline || visited_entries >= MAX_DISCOVERY_ENTRIES {
                partial = true;
                break;
            }
            visited_entries += 1;
            let Ok(entry) = entry else {
                partial = true;
                continue;
            };
            let path = entry.path();
            let Ok(metadata) = fs::symlink_metadata(&path) else {
                partial = true;
                continue;
            };
            if is_reparse_point(&metadata) {
                partial = true;
                continue;
            }
            if metadata.is_dir() {
                stack.push((path, depth + 1));
                continue;
            }
            if !metadata.is_file() || !path.extension().is_some_and(|ext| ext.eq_ignore_ascii_case("jsonl")) {
                continue;
            }
            let Ok(modified) = metadata.modified() else {
                partial = true;
                continue;
            };
            let candidate = Candidate { path, modified, length: metadata.len() };
            if candidates.len() < MAX_SCAN_FILES {
                candidates.push(candidate);
            } else {
                partial = true;
                if let Some((oldest_index, oldest)) = candidates.iter().enumerate().min_by_key(|(_, item)| item.modified) {
                    if candidate.modified > oldest.modified {
                        candidates[oldest_index] = candidate;
                    }
                }
            }
        }
    }

    candidates.sort_by(|left, right| right.modified.cmp(&left.modified));
    (candidates, partial)
}

fn scan_file(candidate: &Candidate, remaining_bytes: &mut u64, deadline: Instant) -> Result<FileScan, ()> {
    let metadata = fs::symlink_metadata(&candidate.path).map_err(|_| ())?;
    if is_reparse_point(&metadata) || !metadata.is_file() {
        return Err(());
    }
    let mut file = File::open(&candidate.path).map_err(|_| ())?;
    let length = file.metadata().map_err(|_| ())?.len();
    let mut result = FileScan::default();
    if length != candidate.length {
        result.partial = true;
    }

    let budget = length.min(MAX_FILE_BYTES).min(*remaining_bytes);
    if budget < length {
        result.partial = true;
    }
    if budget == 0 {
        return Ok(result);
    }
    *remaining_bytes -= budget;

    if length <= budget {
        read_segment(&mut file, 0, budget, false, &mut result, deadline)?;
    } else {
        let head = budget.min(64 * 1024).min(budget / 4);
        let tail = budget.saturating_sub(head);
        if head > 0 {
            read_segment(&mut file, 0, head, false, &mut result, deadline)?;
        }
        if tail > 0 {
            read_segment(&mut file, length - tail, tail, true, &mut result, deadline)?;
        }
    }
    Ok(result)
}

fn read_segment(
    file: &mut File,
    offset: u64,
    length: u64,
    discard_leading_partial_line: bool,
    scan: &mut FileScan,
    deadline: Instant,
) -> Result<(), ()> {
    file.seek(SeekFrom::Start(offset)).map_err(|_| ())?;
    let mut remaining = length;
    let mut buffer = [0u8; BUFFER_BYTES];
    let mut line = Vec::with_capacity(4096);
    let mut segment = Vec::new();
    let mut discard = discard_leading_partial_line;
    let mut overlong = false;
    if discard {
        scan.partial = true;
    }

    while remaining > 0 {
        if Instant::now() >= deadline {
            scan.partial = true;
            break;
        }
        let request = remaining.min(buffer.len() as u64) as usize;
        let read = file.read(&mut buffer[..request]).map_err(|_| ())?;
        if read == 0 {
            scan.partial = true;
            break;
        }
        remaining -= read as u64;
        for &byte in &buffer[..read] {
            if discard {
                if byte == b'\n' {
                    discard = false;
                }
                continue;
            }
            if byte == b'\n' {
                if overlong {
                    overlong = false;
                    line.clear();
                    continue;
                }
                if byte_line_is_gap(&line) {
                    scan.partial = true;
                    flush_segment(scan, &mut segment);
                } else {
                    process_line(&line, scan, &mut segment);
                }
                line.clear();
                continue;
            }
            if overlong {
                continue;
            }
            if line.len() == MAX_LINE_BYTES {
                scan.partial = true;
                overlong = true;
                line.clear();
                flush_segment(scan, &mut segment);
                continue;
            }
            if byte != b'\r' {
                line.push(byte);
            }
        }
    }

    if discard || overlong || !line.is_empty() {
        scan.partial = true;
    }
    flush_segment(scan, &mut segment);
    Ok(())
}

fn byte_line_is_gap(line: &[u8]) -> bool {
    line.iter().all(u8::is_ascii_whitespace)
}

fn flush_segment(scan: &mut FileScan, segment: &mut Vec<(i64, u64)>) {
    if !segment.is_empty() {
        scan.segments.push(std::mem::take(segment));
    }
}

fn process_line(line: &[u8], scan: &mut FileScan, segment: &mut Vec<(i64, u64)>) {
    if !json_depth_within_limit(line) {
        scan.partial = true;
        flush_segment(scan, segment);
        return;
    }
    let Ok(record) = serde_json::from_slice::<Record>(line) else {
        scan.partial = true;
        flush_segment(scan, segment);
        return;
    };

    if record.record_type.as_deref() == Some("session_meta") {
        if let Some(payload) = record.payload.as_ref() {
            if let Some(id) = payload.id.as_deref().filter(|id| !id.is_empty() && id.len() <= 256) {
                let digest: [u8; 32] = Sha256::digest(id.as_bytes()).into();
                if scan.session_id_hash.is_some_and(|known| known != digest) {
                    scan.forked = true;
                } else {
                    scan.session_id_hash = Some(digest);
                }
            } else {
                scan.partial = true;
            }
            if payload.parent_id.as_deref().is_some_and(|id| !id.is_empty()) {
                scan.forked = true;
            }
        }
    }

    if record.record_type.as_deref() != Some("event_msg")
        || record.payload.as_ref().and_then(|payload| payload.payload_type.as_deref()) != Some("token_count")
    {
        return;
    }

    let sample = record
        .timestamp
        .as_deref()
        .and_then(|timestamp| DateTime::parse_from_rfc3339(timestamp).ok())
        .map(|timestamp| timestamp.timestamp())
        .zip(
            record
                .payload
                .and_then(|payload| payload.info)
                .and_then(|info| info.total_token_usage)
                .and_then(|usage| usage.total_tokens),
        );
    if let Some(sample) = sample {
        segment.push(sample);
    } else {
        scan.partial = true;
        flush_segment(scan, segment);
    }
}

fn json_depth_within_limit(bytes: &[u8]) -> bool {
    let (mut depth, mut in_string, mut escaped) = (0usize, false, false);
    for &byte in bytes {
        if in_string {
            if escaped {
                escaped = false;
            } else if byte == b'\\' {
                escaped = true;
            } else if byte == b'"' {
                in_string = false;
            }
            continue;
        }
        match byte {
            b'"' => in_string = true,
            b'{' | b'[' => {
                depth += 1;
                if depth > MAX_JSON_DEPTH {
                    return false;
                }
            }
            b'}' | b']' => depth = depth.saturating_sub(1),
            _ => {}
        }
    }
    true
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
fn is_reparse_point(metadata: &fs::Metadata) -> bool {
    metadata.file_type().is_symlink()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn day_edges(seconds: i64) -> Vec<i64> {
        (0..=12).map(|index| index * seconds / 12).collect()
    }

    #[test]
    fn cumulative_snapshots_repeats_decreases_and_head_tail_gaps_are_lower_bounds() {
        let edges = day_edges(24 * 3600);
        let segments = vec![
            vec![(0, 100), (3_600, 150), (3_600, 150), (5_400, 120), (6_000, 140)],
            vec![(7_200, 900), (10_800, 920)],
        ];
        let data = aggregate_segments(&edges, &segments);
        assert_eq!(data.status, ActivityStatus::Partial);
        assert_eq!(data.total_tokens, Some(90));
        assert_eq!(data.buckets[0], Some(70));
        assert_eq!(data.buckets[1], Some(20));
        assert!(data.buckets[2..].iter().all(Option::is_none));
    }

    #[test]
    fn deltas_crossing_a_bucket_edge_are_not_fabricated_into_either_bucket() {
        let edges = day_edges(24 * 3600);
        let data = aggregate_segments(&edges, &[vec![(7_100, 100), (7_300, 120)]]);
        assert_eq!(data.status, ActivityStatus::Unavailable);
        assert_eq!(data.total_tokens, None);
        assert!(data.buckets.iter().all(Option::is_none));
    }

    #[test]
    fn each_noncontiguous_segment_starts_with_a_fresh_baseline() {
        let edges = day_edges(24 * 3600);
        let data = aggregate_segments(&edges, &[vec![(0, 10), (3_600, 20)], vec![(7_200, 900), (10_800, 920)]]);
        assert_eq!(data.total_tokens, Some(30));
        assert_eq!(data.buckets[0], Some(10));
        assert_eq!(data.buckets[1], Some(20));
    }

    #[test]
    fn validates_local_day_edges_in_seconds_including_half_hour_dst_changes() {
        for seconds in [23 * 3600, 23 * 3600 + 1800, 24 * 3600, 24 * 3600 + 1800, 25 * 3600] {
            assert!(valid_day_edges(&day_edges(seconds)));
        }
        assert!(!valid_day_edges(&(0..=12).collect::<Vec<_>>()));
        assert!(!valid_day_edges(&[0, 1, 2]));
    }

    #[test]
    fn extracts_only_the_canonical_cumulative_total_counter() {
        let line = br#"{"type":"event_msg","timestamp":"2026-10-15T07:45:00Z","payload":{"type":"token_count","info":{"total_token_usage":{"total_tokens":975,"cached_input_tokens":120},"last_token_usage":{"total_tokens":60}},"prompt":"decoy"}}"#;
        let record: Record = serde_json::from_slice(line).unwrap();
        assert_eq!(record.record_type.as_deref(), Some("event_msg"));
        assert_eq!(record.payload.unwrap().info.unwrap().total_token_usage.unwrap().total_tokens, Some(975));
    }
}
