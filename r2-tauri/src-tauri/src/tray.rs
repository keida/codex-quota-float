use std::sync::Mutex;

use serde::Serialize;
use tauri::{
    AppHandle, Emitter, Manager, Runtime, State,
    menu::{Menu, MenuItem, PredefinedMenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
};

const TRAY_ID: &str = "quota-float-tray";
const TRAY_ACTION_EVENT: &str = "tray-action";
const OPEN_ID: &str = "tray-open";
const REFRESH_ID: &str = "tray-refresh";
const EXIT_ID: &str = "tray-exit";

#[derive(Clone, Copy, Serialize)]
#[serde(rename_all = "camelCase")]
pub enum TrayAction {
    Open,
    Refresh,
}

#[derive(Default)]
struct PendingActions {
    frontend_ready: bool,
    actions: Vec<TrayAction>,
}

#[derive(Default)]
pub struct TrayActionState(Mutex<PendingActions>);

#[tauri::command]
pub fn tray_frontend_ready(state: State<'_, TrayActionState>) -> Vec<TrayAction> {
    let Ok(mut pending) = state.0.lock() else {
        return Vec::new();
    };
    pending.frontend_ready = true;
    std::mem::take(&mut pending.actions)
}

fn dispatch<R: Runtime>(app: &AppHandle<R>, action: TrayAction) {
    let state = app.state::<TrayActionState>();
    let Ok(mut pending) = state.0.lock() else {
        return;
    };
    if !pending.frontend_ready {
        pending.actions.push(action);
        return;
    }
    drop(pending);
    let _ = app.emit(TRAY_ACTION_EVENT, action);
}

fn show_window<R: Runtime>(app: &AppHandle<R>) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.unminimize();
        let _ = window.set_focus();
    }
}

fn menu<R: Runtime>(app: &AppHandle<R>, language: &str) -> tauri::Result<Menu<R>> {
    let chinese = language == "zh";
    let open = MenuItem::with_id(app, OPEN_ID, if chinese { "打开" } else { "Open" }, true, None::<&str>)?;
    let refresh = MenuItem::with_id(app, REFRESH_ID, if chinese { "刷新" } else { "Refresh" }, true, None::<&str>)?;
    let separator = PredefinedMenuItem::separator(app)?;
    let exit = MenuItem::with_id(app, EXIT_ID, if chinese { "退出" } else { "Exit" }, true, None::<&str>)?;
    Menu::with_items(app, &[&open, &refresh, &separator, &exit])
}

pub fn initialize<R: Runtime>(app: &AppHandle<R>) -> tauri::Result<()> {
    let icon = app.default_window_icon().cloned().ok_or_else(|| {
        tauri::Error::AssetNotFound("configured application icon".into())
    })?;
    let tray_menu = menu(app, "en")?;
    TrayIconBuilder::with_id(TRAY_ID)
        .icon(icon)
        .menu(&tray_menu)
        .show_menu_on_left_click(false)
        .tooltip("Codex Float R2")
        .on_tray_icon_event(|tray, event| {
            if matches!(event, TrayIconEvent::Click { button: MouseButton::Left, button_state: MouseButtonState::Up, .. }) {
                let app = tray.app_handle().clone();
                show_window(&app);
                dispatch(&app, TrayAction::Open);
            }
        })
        .on_menu_event(|app, event| match event.id().as_ref() {
            OPEN_ID => {
                show_window(app);
                dispatch(app, TrayAction::Open);
            }
            REFRESH_ID => {
                show_window(app);
                dispatch(app, TrayAction::Refresh);
            }
            EXIT_ID => app.exit(0),
            _ => {}
        })
        .build(app)?;
    Ok(())
}

#[tauri::command]
pub fn set_tray_language(app: AppHandle, language: String) -> Result<(), String> {
    if language != "en" && language != "zh" {
        return Err("unsupported tray language".into());
    }
    let id = TRAY_ID.to_string();
    let tray = app.tray_by_id(&id).ok_or_else(|| "tray icon unavailable".to_string())?;
    let tray_menu = menu(&app, &language).map_err(|error| error.to_string())?;
    tray.set_menu(Some(tray_menu)).map_err(|error| error.to_string())?;
    tray.set_tooltip(Some(if language == "zh" { "额度浮窗" } else { "Quota Float" }))
        .map_err(|error| error.to_string())
}
