import { invoke, isTauri } from '@tauri-apps/api/core'
import { listen } from '@tauri-apps/api/event'
import { getCurrentWindow } from '@tauri-apps/api/window'
import type { DashboardSnapshot, Language, View } from './model'

export const inDesktop = () => isTauri()

export type DragCompletion = { released: boolean; snapped: boolean }
export type TrayAction = 'open' | 'refresh'

export async function subscribeTrayActions(callback: (action: TrayAction) => void) {
  if (!inDesktop()) return () => {}
  const unlisten = await listen<TrayAction>('tray-action', event => callback(event.payload))
  try {
    const pending = await invoke<TrayAction[]>('tray_frontend_ready')
    pending.forEach(callback)
  } catch (error) {
    unlisten()
    throw error
  }
  return unlisten
}

export async function setTrayLanguage(language: Language) {
  if (inDesktop()) await invoke('set_tray_language', { language })
}

export async function resizeSurface(surface: View) {
  if (inDesktop()) await invoke('set_surface_size', { surface })
}

export async function startNativeDrag() {
  if (inDesktop()) await getCurrentWindow().startDragging()
}

export async function finishNativeDrag(): Promise<DragCompletion> {
  if (inDesktop()) return invoke<DragCompletion>('finish_window_drag')
  return { released: false, snapped: false }
}

export async function readDashboardSnapshot(dayEdgesUtcUnixSeconds: number[], forceRefresh = false): Promise<DashboardSnapshot | null> {
  if (!inDesktop()) return null
  return invoke<DashboardSnapshot>('read_dashboard_snapshot', { dayEdgesUtcUnixSeconds, forceRefresh })
}

export async function readSystemTheme(): Promise<'light' | 'dark' | null> {
  if (!inDesktop()) return null
  return getCurrentWindow().theme()
}

export async function subscribeSystemTheme(callback: (theme: 'light' | 'dark') => void) {
  if (!inDesktop()) return () => {}
  const unlisten = await getCurrentWindow().onThemeChanged(({ payload }) => callback(payload))
  return unlisten
}

export async function setNativeTheme(theme: 'light' | 'dark' | null) {
  if (inDesktop()) await getCurrentWindow().setTheme(theme)
}
