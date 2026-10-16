use std::sync::Mutex;
use tauri::{LogicalSize, Manager, Size};

mod activity;
mod quota;
mod tray;
mod window_placement;

#[derive(Default)]
struct PlacementState(Mutex<window_placement::Anchor>);

#[cfg(target_os = "windows")]
#[link(name = "user32")]
extern "system" {
    #[link_name = "GetAsyncKeyState"]
    fn get_async_key_state(vkey: i32) -> i16;
}

fn left_mouse_button_is_down() -> bool {
    #[cfg(target_os = "windows")]
    unsafe {
        return get_async_key_state(0x01) < 0;
    }
    #[cfg(not(target_os = "windows"))]
    false
}

fn apply_surface_size(
    window: &tauri::WebviewWindow,
    width: f64,
    height: f64,
    placement: &PlacementState,
) -> Result<(), String> {
    let monitor = window.current_monitor().map_err(|error| error.to_string())?;
    if let Some(monitor) = monitor {
        let work_area = monitor.work_area();
        let area = window_placement::WorkArea {
            x: work_area.position.x,
            y: work_area.position.y,
            width: work_area.size.width,
            height: work_area.size.height,
        };
        let scale = monitor.scale_factor();
        let requested_width = (width * scale).round().max(1.0) as u32;
        let requested_height = (height * scale).round().max(1.0) as u32;
        let (new_width, new_height) = window_placement::fit_surface_size(area, requested_width, requested_height);
        window.set_size(Size::Physical(tauri::PhysicalSize::new(new_width, new_height)))
            .map_err(|error| error.to_string())?;
        let mut anchor = placement.0.lock().map_err(|_| "window placement state unavailable".to_string())?;
        let (next, next_anchor) = window_placement::position_for_surface(area, new_width, new_height, *anchor, scale);
        window.set_position(tauri::Position::Physical(tauri::PhysicalPosition::new(next.x, next.y)))
            .map_err(|error| error.to_string())?;
        *anchor = next_anchor;
    } else {
        window.set_size(Size::Logical(LogicalSize::new(width, height))).map_err(|error| error.to_string())?;
    }
    Ok(())
}

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
struct DashboardSnapshot {
    quota_status: quota::QuotaStatus,
    plan_label: Option<String>,
    quota_windows: Vec<quota::QuotaWindow>,
    quota_fetched_at_unix_seconds: Option<i64>,
    retry_after_seconds: Option<u64>,
    reset_credits: quota::ResetCreditData,
    activity_status: activity::ActivityStatus,
    activity_total_tokens: Option<u64>,
    activity_buckets: Vec<Option<u64>>,
    activity_scanned_at_unix_seconds: Option<i64>,
}

#[tauri::command]
async fn read_dashboard_snapshot(
    day_edges_utc_unix_seconds: Vec<i64>,
    force_refresh: bool,
    reader: tauri::State<'_, quota::QuotaReader>,
) -> Result<DashboardSnapshot, String> {
    if !activity::valid_day_edges(&day_edges_utc_unix_seconds) {
        return Err("invalid local-day range".into());
    }
    let quota = reader.read(force_refresh).await;
    let edges = day_edges_utc_unix_seconds;
    let activity = tauri::async_runtime::spawn_blocking(move || activity::scan_local_activity(&edges))
        .await.map_err(|_| "local activity scan unavailable".to_string())?;
    Ok(DashboardSnapshot {
        quota_status: quota.status,
        plan_label: quota.plan_label,
        quota_windows: quota.windows,
        quota_fetched_at_unix_seconds: quota.fetched_at_unix_seconds,
        retry_after_seconds: quota.retry_after_seconds,
        reset_credits: quota.reset_credits,
        activity_status: activity.status,
        activity_total_tokens: activity.total_tokens,
        activity_buckets: activity.buckets,
        activity_scanned_at_unix_seconds: activity.scanned_at_unix_seconds,
    })
}

#[tauri::command]
fn set_surface_size(app: tauri::AppHandle, surface: String, placement: tauri::State<'_, PlacementState>) -> Result<(), String> {
    let window = app.get_webview_window("main").ok_or("main window unavailable")?;
    let (width, height) = surface_size(&surface).ok_or("unknown surface size")?;
    apply_surface_size(&window, width, height, &placement)
}

fn surface_size(surface: &str) -> Option<(f64, f64)> {
    match surface {
        "widget" => Some((96.0, 96.0)),
        "detail" => Some((340.0, 480.0)),
        "settings" => Some((340.0, 360.0)),
        _ => None,
    }
}

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
struct DragCompletion {
    released: bool,
    snapped: bool,
}

#[tauri::command]
fn finish_window_drag(app: tauri::AppHandle, placement: tauri::State<'_, PlacementState>) -> Result<DragCompletion, String> {
    if left_mouse_button_is_down() { return Ok(DragCompletion { released: false, snapped: false }); }
    let window = app.get_webview_window("main").ok_or("main window unavailable")?;
    let Some(monitor) = window.current_monitor().map_err(|error| error.to_string())? else {
        return Ok(DragCompletion { released: true, snapped: false });
    };
    let position = window.outer_position().map_err(|error| error.to_string())?;
    let size = window.outer_size().map_err(|error| error.to_string())?;
    let work_area = monitor.work_area();
    let area = window_placement::WorkArea {
        x: work_area.position.x,
        y: work_area.position.y,
        width: work_area.size.width,
        height: work_area.size.height,
    };
    let current = window_placement::Rect { x: position.x, y: position.y, width: size.width, height: size.height };
    let (next, anchor) = window_placement::finish_drag(current, area, monitor.scale_factor());
    let snapped = matches!(anchor, window_placement::Anchor::Docked { .. });
    window.set_position(tauri::Position::Physical(tauri::PhysicalPosition::new(next.x, next.y)))
        .map_err(|error| error.to_string())?;
    *placement.0.lock().map_err(|_| "window placement state unavailable".to_string())? = anchor;
    Ok(DragCompletion { released: true, snapped })
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let quota_reader = quota::QuotaReader::new().expect("quota HTTP client initialization failed");
    tauri::Builder::default()
        .manage(quota_reader)
        .manage(PlacementState::default())
        .manage(tray::TrayActionState::default())
        .invoke_handler(tauri::generate_handler![set_surface_size, finish_window_drag, read_dashboard_snapshot, tray::tray_frontend_ready, tray::set_tray_language])
        .setup(|app| {
            if let Some(window) = app.get_webview_window("main") {
                let placement = app.state::<PlacementState>();
                apply_surface_size(&window, 96.0, 96.0, &placement)
                    .map_err(std::io::Error::other)?;
                let close_window = window.clone();
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                        api.prevent_close();
                        let _ = close_window.hide();
                    }
                });
            }
            tray::initialize(&app.handle())?;
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running Quota Float R2 Preview");
}

#[cfg(test)]
mod tests {
    use super::surface_size;

    #[test]
    fn surface_size_matches_compact_detail_and_short_settings_surfaces() {
        assert_eq!(surface_size("widget"), Some((96.0, 96.0)));
        assert_eq!(surface_size("detail"), Some((340.0, 480.0)));
        assert_eq!(surface_size("settings"), Some((340.0, 360.0)));
        assert_eq!(surface_size("unknown"), None);
    }
}
