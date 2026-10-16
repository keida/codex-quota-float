import { useCallback, useEffect, useId, useRef, useState } from 'react'
import { finishNativeDrag, inDesktop, readDashboardSnapshot, readSystemTheme, resizeSurface, setNativeTheme, setTrayLanguage, startNativeDrag, subscribeSystemTheme, subscribeTrayActions, type DragCompletion, type TrayAction } from './bridge'
import { activityChartScale, activityXAxisTickLabels, copy, formatActivityAxisTick, formatCompactTokenCount, localDayEdgesUtcSeconds, type Copy, type DashboardSnapshot, type Language, type Plan, type ResetCreditSnapshot, type ThemeChoice, type View, TOKEN_BARS, TOKEN_TOTAL, visibleQuota } from './model'

const STORAGE_KEY = 'quota-float-r2-preview.preferences'
const REFRESH_INTERVAL_SECONDS = [60, 120, 300] as const
type RefreshIntervalSeconds = (typeof REFRESH_INTERVAL_SECONDS)[number]
type Preferences = { language: Language; plan: Plan; theme: ThemeChoice; refreshIntervalSeconds: RefreshIntervalSeconds }
const defaults: Preferences = { language: 'en', plan: 'plus', theme: 'system', refreshIntervalSeconds: 60 }
const POLL_BACKOFF_MAX_MS = 300_000
const MAX_TIMEOUT_MS = 2_147_000_000
const DRAG_THRESHOLD_PX = 5
const DRAG_RELEASE_POLL_MS = 125

function formatWindowDuration(seconds: number, language: Language, t: Copy) {
  if (seconds === 604800) return t.weeklyFull
  if (seconds === 18000) return t.fiveHourFull
  if (seconds % 86400 === 0) return language === 'zh' ? `${seconds / 86400}天` : `${seconds / 86400}d`
  if (seconds % 3600 === 0) return language === 'zh' ? `${seconds / 3600}小时` : `${seconds / 3600}h`
  if (seconds % 60 === 0) return language === 'zh' ? `${seconds / 60}分钟` : `${seconds / 60}m`
  return language === 'zh' ? `${seconds}秒` : `${seconds}s`
}

function formatResetTime(epochSeconds: number, language: Language) {
  const seconds = Math.max(0, epochSeconds - Math.floor(Date.now() / 1000))
  const days = Math.floor(seconds / 86400)
  const hours = Math.floor((seconds % 86400) / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const remaining = language === 'zh'
    ? days > 0 ? `${days}天${hours}小时` : hours > 0 ? `${hours}小时${minutes}分` : `${minutes}分`
    : days > 0 ? `${days}d ${hours}h` : hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`
  return remaining
}

function formatResetCredits(data: ResetCreditSnapshot | undefined, language: Language, t: Copy) {
  if (!data || data.status === 'unavailable') return t.creditResetNotRead
  if (data.status === 'stale') return t.creditResetStale
  const count = data.availableCount
  if (typeof count !== 'number' || !Number.isSafeInteger(count) || count < 0) return t.creditResetNotRead
  const formattedCount = count.toLocaleString(language === 'zh' ? 'zh-CN' : 'en-AU')
  const expiry = data.expiresAtUnixSeconds
  const validExpiry = typeof expiry === 'number' && Number.isSafeInteger(expiry) && expiry > Math.floor(Date.now() / 1000)
  if (data.status === 'partial' || (count > 0 && !validExpiry)) return t.creditResetPartial.replace('{count}', formattedCount)
  const expiryText = validExpiry ? t.creditResetExpires.replace('{time}', formatResetTime(expiry, language)) : ''
  return `${t.creditResetAvailable.replace('{count}', formattedCount)}${expiryText}`
}

function formatFetchedTime(epochSeconds: number, language: Language) {
  return new Date(epochSeconds * 1000).toLocaleTimeString(language === 'zh' ? 'zh-CN' : 'en-AU', { hour: '2-digit', minute: '2-digit' })
}

function loadPreferences(): Preferences {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}')
    const stored = parsed && typeof parsed === 'object' ? parsed as Partial<Preferences> : {}
    const refreshIntervalSeconds = isRefreshIntervalSeconds(stored.refreshIntervalSeconds)
      ? stored.refreshIntervalSeconds : defaults.refreshIntervalSeconds
    return { ...defaults, ...stored, refreshIntervalSeconds }
  }
  catch { return defaults }
}

function isRefreshIntervalSeconds(value: unknown): value is RefreshIntervalSeconds {
  return value === 60 || value === 120 || value === 300
}

function ProgressOrbit({ value, color, className, testId, strokeWidth }: {
  value: number; color: string; className: string; testId: string; strokeWidth: number
}) {
  const gradientId = `quota-arc-${useId().replace(/:/g, '')}`
  const radius = (100 - strokeWidth) / 2
  return <svg className={`${className} orbit-${color}`} viewBox="0 0 100 100" aria-hidden="true" data-value={value} data-testid={`${testId}-ring`}>
    <defs><linearGradient id={gradientId} x1="50%" y1="0%" x2="50%" y2="100%" gradientTransform="rotate(90 .5 .5)"><stop offset="0%" stopColor={`var(--quota-${color}-arc-bright)`}/><stop offset="100%" stopColor={`var(--quota-${color}-arc-deep)`}/></linearGradient></defs>
    <circle className="orbit-track" cx="50" cy="50" r={radius} strokeWidth={strokeWidth} />
    <circle className="orbit-progress" style={{ stroke: `url(#${gradientId})` }} cx="50" cy="50" r={radius} strokeWidth={strokeWidth} pathLength="100" strokeDasharray="100 100" strokeDashoffset={100 - value} strokeLinecap="round" transform="rotate(-90 50 50)" data-testid={`${testId}-arc`} />
  </svg>
}

function QuotaRing({ value, color, label, reset, windowSeconds, testId, t }: {
  value: number; color: string; label: string; reset?: string; windowSeconds: number; testId: string; t: Copy
}) {
  return <div className={`quota quota-${color}`} data-testid={`quota-${testId}`}>
    <div className="ring" role="img" aria-label={`${label} ${value}%`} data-testid={`detail-${testId}-ring`}>
      <ProgressOrbit value={value} color={color} className="ring-orbit" testId={`detail-${testId}`} strokeWidth={8.6} />
      <div className="ring-center"><strong className="ring-value">{value}%</strong><span className="ring-name" data-testid="quota-window-label">{label} {t.remaining}</span></div>
    </div>
    {reset && <div className="quota-reset">{windowSeconds === 18_000 ? <Timer /> : <CalendarReset />}<span>{t.resets} {reset}</span></div>}
  </div>
}

function ThemeIcon({ theme }: { theme: ThemeChoice }) {
  if (theme === 'light') return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="3.4"/><path d="M12 2v2m0 16v2M4.9 4.9l1.4 1.4m11.4 11.4 1.4 1.4M2 12h2m16 0h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>
  if (theme === 'dark') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20.2 15.2A8.6 8.6 0 0 1 8.8 3.8 8.8 8.8 0 1 0 20.2 15.2Z"/></svg>
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3a9 9 0 0 1 0 18V3Z" fill="currentColor" stroke="none"/><circle cx="12" cy="12" r="8.5"/><path d="M12 3v18"/></svg>
}

export default function App() {
  const native = inDesktop()
  const [prefs, setPrefs] = useState(loadPreferences)
  const [view, setView] = useState<View>('widget')
  const viewRef = useRef(view)
  viewRef.current = view
  const [sampleRefreshedAt, setSampleRefreshedAt] = useState<Date | null>(null)
  const [dashboard, setDashboard] = useState<DashboardSnapshot | null>(null)
  const [nativeRefreshing, setNativeRefreshing] = useState(false)
  const [nativeDragging, setNativeDragging] = useState(false)
  const [nativeReadFailed, setNativeReadFailed] = useState(false)
  const [manualRefreshInProgress, setManualRefreshInProgress] = useState(false)
  const [manualRefreshFeedback, setManualRefreshFeedback] = useState<'success' | 'partial' | 'stale' | 'unavailable' | 'failed' | null>(null)
  const [systemTheme, setSystemTheme] = useState<'light' | 'dark'>('dark')
  const [keyboardOpened, setKeyboardOpened] = useState(false)
  const clickThroughOrigin = useRef<{ x: number; y: number } | null>(null)
  const restoreWidgetFocus = useRef(false)
  const skipWidgetFocus = useRef(false)
  const focusSettingsOnReturn = useRef(false)
  const liveRequest = useRef<Promise<DashboardSnapshot | null> | null>(null)
  const manualRefreshRequested = useRef(false)
  const pollTimer = useRef<number | undefined>(undefined)
  const pollSchedule = useRef<{ healthy: boolean; retryAfterMs: number } | null>(null)
  const refreshIntervalMs = useRef(prefs.refreshIntervalSeconds * 1000)
  refreshIntervalMs.current = prefs.refreshIntervalSeconds * 1000
  const dragGesture = useRef<{ pointerId: number; x: number; y: number; started: boolean } | null>(null)
  const nativeDragActive = useRef(false)
  const dragFinishRequest = useRef<Promise<DragCompletion> | null>(null)
  const dragWatchdogTimer = useRef<number | undefined>(undefined)
  const hoverSuppressed = useRef(false)
  const pointerInsideSurface = useRef(false)
  const failureCount = useRef(0)
  const widgetButton = useRef<HTMLButtonElement>(null)
  const settingsButton = useRef<HTMLButtonElement>(null)
  const surfaceElement = useRef<HTMLElement>(null)
  const leaveTimer = useRef<number | undefined>(undefined)
  const suppressResizeHover = useRef(false)
  const manualFeedbackTimer = useRef<number | undefined>(undefined)

  function showManualRefreshFeedback(outcome: NonNullable<typeof manualRefreshFeedback>) {
    window.clearTimeout(manualFeedbackTimer.current)
    setManualRefreshFeedback(outcome)
    manualFeedbackTimer.current = window.setTimeout(() => {
      manualFeedbackTimer.current = undefined
      setManualRefreshFeedback(null)
    }, 3500)
  }

  function scheduleNativeDragCheck() {
    window.clearTimeout(dragWatchdogTimer.current)
    if (!nativeDragActive.current) return
    dragWatchdogTimer.current = window.setTimeout(() => {
      dragWatchdogTimer.current = undefined
      void finishNativeDragIfReleased()
    }, DRAG_RELEASE_POLL_MS)
  }

  function finishNativeDragIfReleased(): Promise<DragCompletion> {
    if (!nativeDragActive.current) return Promise.resolve({ released: false, snapped: false })
    if (dragFinishRequest.current) return dragFinishRequest.current
    const request = finishNativeDrag()
      .then(completion => {
        if (completion.released) {
          nativeDragActive.current = false
          setNativeDragging(false)
          hoverSuppressed.current = false
          dragGesture.current = null
          window.clearTimeout(dragWatchdogTimer.current)
          dragWatchdogTimer.current = undefined
          if (completion.snapped && viewRef.current === 'detail') collapseToWidget()
        }
        return completion
      })
      .catch(() => ({ released: false, snapped: false }))
      .finally(() => {
        dragFinishRequest.current = null
        if (nativeDragActive.current) scheduleNativeDragCheck()
      })
    dragFinishRequest.current = request
    return request
  }

  function beginNativeDrag(event: React.PointerEvent<HTMLElement>) {
    if (!native || !event.isPrimary || event.button !== 0 || nativeDragActive.current || dragGesture.current) return
    const target = event.target
    if (!(target instanceof Element) || !target.closest('[data-drag-region]')) return
    if (target.closest('button:not([data-drag-handle]), input, select, textarea, a, [role="button"]:not([data-drag-handle]), [data-no-drag]')) return
    try {
      event.currentTarget.setPointerCapture(event.pointerId)
    } catch {
      return
    }
    dragGesture.current = { pointerId: event.pointerId, x: event.clientX, y: event.clientY, started: false }
    hoverSuppressed.current = true
    window.clearTimeout(leaveTimer.current)
  }

  function moveNativeDrag(event: React.PointerEvent<HTMLElement>) {
    const gesture = dragGesture.current
    if (!native || !gesture || gesture.pointerId !== event.pointerId || gesture.started) return
    if (Math.hypot(event.clientX - gesture.x, event.clientY - gesture.y) < DRAG_THRESHOLD_PX) return
    gesture.started = true
    nativeDragActive.current = true
    setNativeDragging(true)
    hoverSuppressed.current = true
    event.preventDefault()
    void startNativeDrag().catch(() => {})
    scheduleNativeDragCheck()
  }

  function endNativeDragPointer(event: React.PointerEvent<HTMLElement>) {
    const gesture = dragGesture.current
    if (!gesture || gesture.pointerId !== event.pointerId) return
    try {
      if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId)
    } catch { /* Capture may already have been transferred by the window manager. */ }
    if (!gesture.started) {
      dragGesture.current = null
      hoverSuppressed.current = false
      return
    }
    void finishNativeDragIfReleased()
  }

  function cancelNativeDragPointer(event: React.PointerEvent<HTMLElement>) {
    const gesture = dragGesture.current
    if (!gesture || gesture.pointerId !== event.pointerId) return
    if (gesture.started) {
      // Pointer cancellation is not proof that the OS mouse button has been released.
      dragGesture.current = null
      return
    }
    dragGesture.current = null
    hoverSuppressed.current = false
  }

  const refreshLive = useCallback(async (forceRefresh = false) => {
    if (!native) return null
    if (liveRequest.current) {
      if (forceRefresh) {
        manualRefreshRequested.current = true
        window.clearTimeout(manualFeedbackTimer.current)
        manualFeedbackTimer.current = undefined
        setManualRefreshFeedback(null)
        setManualRefreshInProgress(true)
      }
      return liveRequest.current
    }
    if (forceRefresh) {
      window.clearTimeout(manualFeedbackTimer.current)
      manualFeedbackTimer.current = undefined
      setManualRefreshFeedback(null)
      setManualRefreshInProgress(true)
    }
    window.clearTimeout(pollTimer.current)
    pollTimer.current = undefined
    pollSchedule.current = null
    setNativeRefreshing(true)

    const request = (async () => {
      try {
        const result = await readDashboardSnapshot(localDayEdgesUtcSeconds(), forceRefresh)
        if (!result) throw new Error('No native snapshot')
        setDashboard(result)
        setNativeReadFailed(false)
        if (forceRefresh || manualRefreshRequested.current) {
          showManualRefreshFeedback(result.quotaStatus === 'available' ? 'success'
            : result.quotaStatus === 'partial' ? 'partial'
              : result.quotaStatus === 'stale' ? 'stale' : 'unavailable')
        }
        const healthy = result.quotaStatus === 'available' || result.quotaStatus === 'partial'
        failureCount.current = healthy ? 0 : failureCount.current + 1
        const backoff = healthy
          ? refreshIntervalMs.current
          : Math.min(5_000 * 2 ** (failureCount.current - 1), POLL_BACKOFF_MAX_MS)
        const retryAfter = typeof result.retryAfterSeconds === 'number' && Number.isFinite(result.retryAfterSeconds)
          ? Math.max(0, result.retryAfterSeconds) * 1000
          : 0
        scheduleNextPoll(Math.max(backoff, retryAfter), healthy, retryAfter)
        return result
      } catch {
        setDashboard(null)
        setNativeReadFailed(true)
        if (forceRefresh || manualRefreshRequested.current) showManualRefreshFeedback('failed')
        failureCount.current += 1
        scheduleNextPoll(Math.min(5_000 * 2 ** (failureCount.current - 1), POLL_BACKOFF_MAX_MS), false)
        return null
      } finally {
        liveRequest.current = null
        setNativeRefreshing(false)
        if (forceRefresh || manualRefreshRequested.current) setManualRefreshInProgress(false)
        manualRefreshRequested.current = false
      }
    })()
    liveRequest.current = request
    return request
  }, [native])

  function scheduleNextPoll(delayMs: number, healthy: boolean, retryAfterMs = 0) {
    window.clearTimeout(pollTimer.current)
    const dueAt = Date.now() + Math.max(delayMs, 0)
    pollSchedule.current = { healthy, retryAfterMs }
    const waitUntilDue = () => {
      const remaining = dueAt - Date.now()
      pollTimer.current = window.setTimeout(() => {
        if (Date.now() < dueAt) { waitUntilDue(); return }
        pollTimer.current = undefined
        pollSchedule.current = null
        void refreshLive()
      }, Math.min(Math.max(remaining, 0), MAX_TIMEOUT_MS))
    }
    waitUntilDue()
  }

  const t = copy[prefs.language]
  const intervalLabels: Record<RefreshIntervalSeconds, string> = { 60: t.interval60, 120: t.interval120, 300: t.interval300 }
  const quotaSnapshotUsable = dashboard?.quotaStatus === 'available' || dashboard?.quotaStatus === 'partial' || dashboard?.quotaStatus === 'stale'
  const rawQuotaWindows = Array.isArray(dashboard?.quotaWindows) ? dashboard.quotaWindows : []
  const nativeQuotaItems = (quotaSnapshotUsable ? rawQuotaWindows : [])
    .filter(({ windowSeconds, remainingPercent }) => Number.isFinite(windowSeconds) && windowSeconds > 0 && Number.isFinite(remainingPercent))
    .slice()
    .sort((left, right) => left.windowSeconds - right.windowSeconds || (left.resetsAtUnixSeconds ?? 0) - (right.resetsAtUnixSeconds ?? 0))
    .map(item => ({
      key: `${item.windowSeconds}:${item.resetsAtUnixSeconds ?? 'none'}`,
      testId: `window-${item.windowSeconds}-${item.resetsAtUnixSeconds ?? 'none'}`,
      windowSeconds: item.windowSeconds,
      value: Math.max(0, Math.min(100, item.remainingPercent)),
      color: item.windowSeconds <= 86400 ? 'mint' as const : 'violet' as const,
      label: formatWindowDuration(item.windowSeconds, prefs.language, t),
      reset: item.resetsAtUnixSeconds === null ? undefined : formatResetTime(item.resetsAtUnixSeconds, prefs.language),
    }))
  const quotaItems = native ? nativeQuotaItems : visibleQuota(prefs.plan).map(item => ({
    key: item.key,
    testId: item.key,
    windowSeconds: item.key === 'fiveHour' ? 18_000 : 604_800,
    value: item.value,
    color: item.color as 'mint' | 'violet',
    label: item.key === 'fiveHour' ? t.fiveHourFull : t.weeklyFull,
    reset: prefs.plan === 'plus' ? t[item.reset] : undefined,
  }))
  const rawActivityBuckets = dashboard?.activityBuckets
  const activityBuckets = native
    ? dashboard?.activityStatus !== 'unavailable' && Array.isArray(rawActivityBuckets) && rawActivityBuckets.length === 12
      ? rawActivityBuckets.map(value => typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 ? value : null)
      : Array<number | null>(12).fill(null)
    : TOKEN_BARS
  const knownActivityValues = activityBuckets.filter((value): value is number => value !== null)
  const peakTokenBucket = knownActivityValues.length ? Math.max(...knownActivityValues) : 0
  const peakIndex = peakTokenBucket > 0 ? activityBuckets.findIndex(value => value === peakTokenBucket) : -1
  const chartScale = native ? activityChartScale(peakTokenBucket) : 3000
  const peakLabelText = peakTokenBucket.toLocaleString(prefs.language === 'zh' ? 'zh-CN' : 'en-AU')
  const peakVisibleLabel = formatCompactTokenCount(peakTokenBucket)
  const xAxisLabels = activityXAxisTickLabels(localDayEdgesUtcSeconds(), prefs.language)
  const chartEmptyText = native && knownActivityValues.length === 0
    ? dashboard?.activityStatus === 'partial' ? t.activityPartialEmpty : t.activityEmpty
    : null
  const chartAccessibleLabel = !native ? t.chartAccessible
    : `${t.liveChartAccessible}${dashboard?.activityStatus === 'partial' ? `; ${t.activityCoveragePartial}` : ''}${peakIndex >= 0 ? `; ${t.peak}: ${peakLabelText} ${t.tokenUnit}` : ''}`
  const compactFiveHour = quotaItems.find(({ key }) => key === 'fiveHour')?.value ?? 0
  const compactWeekly = quotaItems.find(({ label }) => label === t.weeklyFull)?.value ?? 0
  const liveTotal = dashboard?.activityTotalTokens
  const validLiveTotal = dashboard?.activityStatus !== 'unavailable' && typeof liveTotal === 'number' && Number.isSafeInteger(liveTotal) && liveTotal >= 0 ? liveTotal : null
  const activityTotalText = !native
    ? formatCompactTokenCount(TOKEN_TOTAL)
    : validLiveTotal === null
      ? dashboard?.activityStatus === 'unavailable' ? t.activityUnavailable : dashboard?.activityStatus === 'partial' ? t.activityPartialUnknown : t.unknown
      : `${dashboard?.activityStatus === 'partial' ? '≥' : ''}${formatCompactTokenCount(validLiveTotal, dashboard?.activityStatus === 'partial')}`
  const activityTotalExactText = native
    ? validLiveTotal === null ? activityTotalText : `${dashboard?.activityStatus === 'partial' ? '≥' : ''}${validLiveTotal.toLocaleString(prefs.language === 'zh' ? 'zh-CN' : 'en-AU')} ${t.tokenUnit}`
    : `${TOKEN_TOTAL.toLocaleString(prefs.language === 'zh' ? 'zh-CN' : 'en-AU')} ${t.tokenUnit}`
  const activeTheme = prefs.theme === 'system' ? systemTheme : prefs.theme
  const collapseLabel = prefs.language === 'zh' ? '收起到小组件' : 'Minimise to widget'
  const expanded = view !== 'widget'
  const compactAccessibleLabel = native
    ? `${t.openDetails}, ${quotaItems.slice(0, 2).map(item => `${item.label} ${item.value}%`).join(', ') || t.unknown}${dashboard?.quotaStatus === 'stale' ? `, ${t.stale}` : ''}`
    : prefs.plan === 'plus'
      ? `${t.openDetails}, ${t.fiveHourShort} ${compactFiveHour}%, ${t.weeklyShort} ${compactWeekly}%`
      : `${t.openDetails}, ${compactWeekly}% ${t.weeklyShort}`
  const fetchedAtText = dashboard?.quotaFetchedAtUnixSeconds === null || dashboard?.quotaFetchedAtUnixSeconds === undefined
    ? null : formatFetchedTime(dashboard.quotaFetchedAtUnixSeconds, prefs.language)
  const freshnessText = fetchedAtText ? `${t.updated} ${fetchedAtText}` : t.updated
  const quotaStatusText = nativeReadFailed
    ? `${t.refreshFailed}${fetchedAtText ? ` · ${freshnessText}` : ''}`
    : !dashboard ? nativeRefreshing ? t.loading : t.unknown
      : dashboard.quotaStatus === 'available' ? freshnessText
        : dashboard.quotaStatus === 'partial' ? `${t.partial}${fetchedAtText ? ` · ${freshnessText}` : ''}`
          : dashboard.quotaStatus === 'stale' ? `${t.stale}${fetchedAtText ? ` · ${freshnessText}` : ''}`
          : dashboard.quotaStatus === 'signedOut' ? t.signedOut
            : dashboard.quotaStatus === 'rateLimited' ? t.rateLimited : t.unavailableLive
  const manualRefreshFeedbackText = manualRefreshInProgress ? t.refreshing
    : manualRefreshFeedback === 'success' ? t.refreshComplete
    : manualRefreshFeedback === 'partial' ? t.refreshIncompletePartial
      : manualRefreshFeedback === 'stale' ? t.refreshIncompleteStale
        : manualRefreshFeedback === 'unavailable' ? t.refreshIncompleteUnavailable
          : manualRefreshFeedback === 'failed' ? t.refreshFailed : null

  useEffect(() => { localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs)) }, [prefs])
  useEffect(() => {
    if (!native) return
    void refreshLive()
    return () => {
      window.clearTimeout(pollTimer.current)
      window.clearTimeout(manualFeedbackTimer.current)
      pollSchedule.current = null
    }
  }, [native, refreshLive])
  useEffect(() => {
    const scheduled = pollSchedule.current
    if (!native || liveRequest.current || !scheduled?.healthy) return
    scheduleNextPoll(Math.max(refreshIntervalMs.current, scheduled.retryAfterMs), true, scheduled.retryAfterMs)
  }, [native, prefs.refreshIntervalSeconds])
  useEffect(() => {
    document.documentElement.lang = prefs.language === 'zh' ? 'zh-CN' : 'en'
    document.documentElement.dataset.previewBackdrop = String(!native)
    document.title = native ? 'Quota Float' : 'Quota Float — browser visual preview (CSS backdrop)'
  }, [prefs.language, native])
  useEffect(() => {
    if (view === 'detail' && (keyboardOpened || focusSettingsOnReturn.current)) {
      settingsButton.current?.focus()
      setKeyboardOpened(false)
      focusSettingsOnReturn.current = false
    }
  }, [view, keyboardOpened])
  useEffect(() => {
    if (view === 'widget' && restoreWidgetFocus.current) {
      restoreWidgetFocus.current = false
      skipWidgetFocus.current = true
      widgetButton.current?.focus()
    }
  }, [view])
  useEffect(() => {
    let unlisten = () => {}
    void readSystemTheme().then(theme => { if (theme) setSystemTheme(theme) })
    void subscribeSystemTheme(setSystemTheme).then(stop => { unlisten = stop })
    return () => unlisten()
  }, [])
  useEffect(() => { void setTrayLanguage(prefs.language) }, [native, prefs.language])
  useEffect(() => {
    if (!native) return
    let active = true
    let unlisten = () => {}
    const handleAction = (action: TrayAction) => {
      if (!active) return
      if (viewRef.current === 'settings') {
        if (action === 'refresh') void refreshLive(true)
        return
      }
      window.clearTimeout(leaveTimer.current)
      if (action === 'open') {
        pointerInsideSurface.current = false
        suppressResizeHover.current = false
        collapseToWidget()
      }
      else {
        setView('detail')
        void refreshLive(true)
      }
    }
    queueMicrotask(() => {
      if (!active) return
      void subscribeTrayActions(handleAction).then(stop => {
        if (active) unlisten = stop
        else stop()
      }).catch(() => {})
    })
    return () => {
      active = false
      unlisten()
    }
  }, [native, refreshLive])
  useEffect(() => { void resizeSurface(view) }, [view])
  useEffect(() => { void setNativeTheme(prefs.theme === 'system' ? null : prefs.theme) }, [prefs.theme])
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') collapseToWidget()
      if ((event.key === 'Enter' || event.key === ' ') && document.activeElement?.getAttribute('data-testid') === 'widget') {
        event.preventDefault(); setView('detail')
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])
  useEffect(() => {
    const onBlur = () => {
      if (nativeDragActive.current) void finishNativeDragIfReleased()
      else if (dragGesture.current && !dragGesture.current.started) {
        dragGesture.current = null
        hoverSuppressed.current = false
      }
    }
    window.addEventListener('blur', onBlur)
    return () => {
      window.removeEventListener('blur', onBlur)
      window.clearTimeout(dragWatchdogTimer.current)
    }
  }, [])
  useEffect(() => {
    const onPointerMove = (event: PointerEvent) => {
      if (!suppressResizeHover.current) return
      leaveSurface(event.clientX, event.clientY)
    }
    window.addEventListener('pointermove', onPointerMove, true)
    return () => window.removeEventListener('pointermove', onPointerMove, true)
  }, [])

  function update<K extends keyof Preferences>(key: K, value: Preferences[K]) {
    setPrefs(previous => ({ ...previous, [key]: value }))
  }
  function collapseToWidget() {
    suppressResizeHover.current = pointerInsideSurface.current
    window.clearTimeout(leaveTimer.current)
    setView(current => {
      if (current !== 'widget') restoreWidgetFocus.current = true
      return 'widget'
    })
  }
  function enterDetail(event?: React.MouseEvent<HTMLButtonElement> | React.PointerEvent<HTMLButtonElement>) {
    if (event) pointerInsideSurface.current = true
    if (hoverSuppressed.current || suppressResizeHover.current) return
    window.clearTimeout(leaveTimer.current)
    if (event && view === 'widget') clickThroughOrigin.current = { x: event.clientX, y: event.clientY }
    setView(current => current === 'settings' ? current : 'detail')
  }
  function returnToDetail() {
    suppressResizeHover.current = true
    focusSettingsOnReturn.current = true
    setView('detail')
  }
  function scheduleSurfaceLeave() {
    window.clearTimeout(leaveTimer.current)
    leaveTimer.current = window.setTimeout(() => setView(current => current === 'settings' ? current : 'widget'), 120)
  }
  function leaveSurface(clientX?: number, clientY?: number) {
    if (suppressResizeHover.current) {
      const bounds = surfaceElement.current?.getBoundingClientRect()
      if (!bounds || clientX === undefined || clientY === undefined) return
      if (clientX >= bounds.left && clientX < bounds.right && clientY >= bounds.top && clientY < bounds.bottom) return
      suppressResizeHover.current = false
    }
    if (!hoverSuppressed.current && view !== 'settings') scheduleSurfaceLeave()
  }
  function refreshSample() { setSampleRefreshedAt(new Date()) }

  return <main ref={surfaceElement} className={`surface theme-${activeTheme} ${expanded ? 'is-expanded' : 'is-compact'} ${view === 'settings' ? 'is-settings' : ''}`} data-testid="surface" data-drag-region="true" data-dragging={nativeDragging}
    onPointerDown={beginNativeDrag} onPointerUp={endNativeDragPointer} onPointerCancel={cancelNativeDragPointer}
    onPointerLeave={event => { pointerInsideSurface.current = false; leaveSurface(event.clientX, event.clientY) }} onMouseLeave={event => { pointerInsideSurface.current = false; leaveSurface(event.clientX, event.clientY) }} onPointerEnter={() => { pointerInsideSurface.current = true; if (!hoverSuppressed.current) window.clearTimeout(leaveTimer.current) }} onMouseEnter={() => { pointerInsideSurface.current = true; if (!hoverSuppressed.current) window.clearTimeout(leaveTimer.current) }}
    onPointerMove={event => { const origin = clickThroughOrigin.current; if (origin && Math.hypot(event.clientX - origin.x, event.clientY - origin.y) > 4) clickThroughOrigin.current = null; moveNativeDrag(event) }}
    onContextMenu={event => event.preventDefault()}
    onClickCapture={event => { if (nativeDragActive.current) { event.preventDefault(); event.stopPropagation(); return } if (clickThroughOrigin.current) { clickThroughOrigin.current = null; if (event.detail !== 0) { event.preventDefault(); event.stopPropagation() } } }}>
    {view === 'widget' ? <button ref={widgetButton} className="widget" data-testid="widget" data-drag-handle="true" aria-label={compactAccessibleLabel}
      onFocus={() => { if (skipWidgetFocus.current) { skipWidgetFocus.current = false; return } setKeyboardOpened(true); enterDetail() }}
      onMouseEnter={enterDetail} onPointerEnter={enterDetail} onClick={() => setView('detail')}>
      {native ? quotaItems.length ? <>
        {quotaItems.slice(0, 2).map((item, index) => <ProgressOrbit key={item.key} value={item.value} color={item.color}
          className={`compact-orbit ${quotaItems.length === 1 ? 'pro-orbit' : index === 0 ? 'compact-outer' : 'compact-inner'}`}
          testId={`compact-live-${index}`} strokeWidth={7.6} />)}
        <span className={`compact-values ${quotaItems.length === 1 && quotaItems[0]?.windowSeconds === 604800 ? 'compact-pro' : ''}`}>
          {quotaItems.length === 1 && quotaItems[0]?.windowSeconds === 604800
            ? <><b data-testid="compact-weekly-value">{quotaItems[0].value}%</b><i>{t.weeklyShort}</i></>
            : <>{quotaItems.slice(0, 2).map((item, index) => <span key={item.key} data-testid={item.windowSeconds === 18000 ? 'compact-five-hour-reading' : item.windowSeconds === 604800 ? 'compact-weekly-reading' : undefined} className={`compact-reading ${item.windowSeconds === 604800 || index === 1 ? 'compact-reading-weekly' : ''}`}>
              <i>{item.windowSeconds === 604800 ? t.weeklyShort : item.windowSeconds === 18000 ? t.fiveHourShort : item.label}</i><b>{item.value}%</b>
            </span>)}{quotaItems.length > 2 && <i>+{quotaItems.length - 2}</i>}</>}
        </span>
      </> : <span className="compact-values compact-empty" data-testid="compact-quota-status">{dashboard?.planLabel ?? (nativeRefreshing ? t.loading : t.unknown)}</span>
        : prefs.plan === 'plus' ? <>
        <ProgressOrbit value={compactFiveHour} color="mint" className="compact-orbit compact-outer" testId="compact-five-hour" strokeWidth={7.5} />
        <ProgressOrbit value={compactWeekly} color="violet" className="compact-orbit compact-inner" testId="compact-weekly" strokeWidth={7.8} />
        <span className="compact-values"><span className="compact-reading" data-testid="compact-five-hour-reading"><i>{t.fiveHourShort}</i><b>{compactFiveHour}%</b></span><span className="compact-reading compact-reading-weekly" data-testid="compact-weekly-reading"><i>{t.weeklyShort}</i><b>{compactWeekly}%</b></span></span>
      </> : <>
        <ProgressOrbit value={compactWeekly} color="violet" className="compact-orbit pro-orbit" testId="compact-weekly" strokeWidth={7.7} />
        <span className="compact-values compact-pro"><b data-testid="compact-weekly-value">{compactWeekly}%</b><i>{t.weeklyShort}</i></span>
      </>}
      {native && dashboard?.quotaStatus === 'stale' && <span className="compact-stale-cue" data-testid="compact-stale-cue" aria-hidden="true" />}
    </button> : view === 'detail' ? <section className="detail" data-testid="detail" aria-label={t.quotaDetails}>
      <header className="detail-header" data-drag-region="true">
        <div className="product-heading"><h1>Codex</h1>{native
          ? <span className="native-plan-label" data-testid="plan-label" title={dashboard?.planLabel ?? t.unknown}>{dashboard?.planLabel ?? t.unknown}</span>
          : <div className="plan-switch" role="group" aria-label={t.planSelection}>
          <button data-testid="plan-plus" aria-pressed={prefs.plan === 'plus'} className={prefs.plan === 'plus' ? 'selected' : ''} onClick={() => update('plan', 'plus')}>{t.plus}</button>
          <button data-testid="plan-pro" aria-pressed={prefs.plan === 'pro'} className={prefs.plan === 'pro' ? 'selected' : ''} onClick={() => update('plan', 'pro')}>{t.pro}</button>
        </div>}</div>
        <div className="icon-toolbar">
          {!native && <button className="icon-button" data-testid="pin-action" aria-label={t.pinUnavailable} disabled><Pin /></button>}
          {native
            ? <button className="icon-button" data-testid="refresh-live" aria-label={manualRefreshInProgress ? t.refreshing : t.refreshLive} title={manualRefreshInProgress ? t.refreshing : t.refreshLive} aria-busy={manualRefreshInProgress} disabled={nativeRefreshing} onClick={() => void refreshLive(true)}><Refresh /></button>
            : <button className="icon-button" data-testid="refresh-sample" aria-label={t.refresh} title={t.refresh} onClick={refreshSample}><Refresh /></button>}
          <button className="icon-button" data-testid="collapse-to-widget" aria-label={collapseLabel} title={collapseLabel} onClick={collapseToWidget}><Minimise /></button>
          <button ref={settingsButton} className="icon-button" data-testid="settings-open" aria-label={t.settings} onClick={() => setView('settings')}><Gear /></button>
        </div>
      </header>
      <div className="quota-section-title">{t.quotaRemaining}</div>
      <div className={`quota-grid ${native ? `quota-live-grid ${native && quotaItems.length === 1 && quotaItems[0]?.windowSeconds === 604800 ? 'quota-live-weekly' : ''}` : prefs.plan === 'pro' ? 'quota-pro' : 'quota-plus'}`}>
        {quotaItems.map(item => <QuotaRing key={item.key} value={item.value} color={item.color} label={item.label} reset={native && quotaItems.length === 1 && item.windowSeconds === 604800 ? undefined : item.reset} windowSeconds={item.windowSeconds} testId={item.testId} t={t} />)}
        {native && quotaItems.length === 0 && <span className="quota-empty" data-testid="quota-empty">{dashboard ? t.noQuotaWindows : t.loading}</span>}
        {native && quotaItems.length === 1 && quotaItems[0]?.windowSeconds === 604800 && <div className="pro-reset-panel" data-testid="weekly-reset-panel"><CalendarReset /><span>{t.resets}</span><strong>{quotaItems[0].reset ?? t.resetUnavailable}</strong></div>}
        {!native && prefs.plan === 'pro' && <div className="pro-reset-panel"><CalendarReset /><span>{t.resets}</span><strong>{t.days}</strong></div>}
      </div>
      <div className="status-row" data-testid="quota-status-row">
        {native
          ? <><span className="status-left"><span>{t.quotaStatus}</span></span><span className="status-right"><span className={`status-dot ${dashboard?.quotaStatus === 'available' || dashboard?.quotaStatus === 'partial' ? 'status-dot-live' : ''}`} data-testid="quota-status-dot" /><span data-testid="quota-status" aria-live="polite">{manualRefreshFeedbackText ? <span data-testid="refresh-feedback" role="status">{manualRefreshFeedbackText}</span> : quotaStatusText}</span></span></>
          : <><span className="status-left"><WindowIcon /><span>{t.presence}</span></span><span className="status-right"><span className="status-dot" />{t.notConnected}<i />{sampleRefreshedAt ? <span aria-live="polite">{t.refreshedLocally} · {sampleRefreshedAt.toLocaleTimeString(prefs.language === 'zh' ? 'zh-CN' : 'en-AU', { hour: '2-digit', minute: '2-digit' })}</span> : t.demo}</span></>}
      </div>
      <section className="activity" aria-label={t.tokens}>
        <div className="activity-heading"><h2>{t.tokens}</h2><span className="range-label">{t.today}</span></div>
        <div className="activity-total" data-testid="activity-total"><strong title={activityTotalExactText} aria-label={activityTotalExactText}>{activityTotalText}{activityTotalText === t.unknown || activityTotalText === t.activityUnavailable || activityTotalText === t.activityPartialUnknown ? null : <> <small>{t.tokenUnit}</small></>}</strong><span data-testid="device-label">{t.device}</span></div>
        <div className="chart" role="img" aria-label={chartAccessibleLabel} data-testid="token-chart">
          <div className="y-axis" aria-hidden="true">{[chartScale, chartScale * 2 / 3, chartScale / 3, 0].map((tick, index) => <span key={index}>{formatActivityAxisTick(tick)}</span>)}</div>
          <div className="chart-main">
            <div className="plot" data-testid="token-plot" data-chart-scale={chartScale}>
              <div className="grid-lines" aria-hidden="true"><i /><i /><i /><i /></div>
              {activityBuckets.map((value, index) => <span key={index} data-testid="token-bar" data-value={value === null ? 'unknown' : value}
                data-peak={index === peakIndex ? 'true' : undefined}
                data-peak-edge={index === peakIndex ? index === 0 ? 'start' : index === activityBuckets.length - 1 ? 'end' : 'middle' : undefined}
                aria-hidden={index === peakIndex ? undefined : true} className={`bar-slot ${[2, 5, 7, 9, 11].includes(index) ? 'bar-violet' : 'bar-blue'} ${value === null ? 'bar-unknown' : ''}`}
                style={{ '--bar-height': `${value === null ? 0 : value / chartScale * 100}%` } as React.CSSProperties}>
                <i aria-hidden="true" />
                {index === peakIndex && <span className="peak-label" data-testid="peak-label" title={`${t.peak}: ${peakLabelText} ${t.tokenUnit}`} aria-label={`${t.peak}: ${peakLabelText} ${t.tokenUnit}`}>{peakVisibleLabel}</span>}
              </span>)}
              {chartEmptyText && <span className="chart-empty-state" data-testid="chart-empty-state">{chartEmptyText}</span>}
            </div>
            <div className="x-axis" aria-hidden="true">{xAxisLabels.map((label, index) => <span key={index} data-testid={`axis-tick-${index}`} data-tick-position={`${index * 25}%`} style={{ '--tick-position': `${index * 25}%` } as React.CSSProperties}>{label}</span>)}</div>
            <div className="chart-caption" data-testid="chart-caption">{native && dashboard?.activityStatus === 'partial' ? `${t.activityCoveragePartial} · ` : ''}{native ? t.liveTokensPerBucket : t.tokensPerBucket}</div>
          </div>
        </div>
      </section>
      <footer className="detail-footer" data-testid="reset-opportunities">
        {native ? <span data-testid="credit-reset-status">{formatResetCredits(dashboard?.resetCredits, prefs.language, t)}</span> : <><strong>{t.resetOpportunities}</strong><i /><span>{t.resetExpiry}</span><ChevronRight /></>}
      </footer>
    </section> : <section className="settings-page" data-testid="settings-panel" aria-labelledby="settings-title">
      <header className="settings-header" data-drag-region="true"><h2 id="settings-title">{t.settings}</h2><button className="icon-button close-button" data-testid="settings-close" aria-label={t.done} onClick={returnToDetail}><Close /></button></header>
      <div className="theme-setting"><span className="setting-label">{t.theme}</span><div className="theme-choices" role="group" aria-label={t.theme}>
        {(['system', 'light', 'dark'] as const).map(theme => <button key={theme} data-testid={`theme-${theme}`} className={`theme-choice ${prefs.theme === theme ? 'selected' : ''}`} aria-pressed={prefs.theme === theme} onClick={() => update('theme', theme)}><ThemeIcon theme={theme} /><span>{t[theme]}</span></button>)}
      </div></div>
      <label className="setting-row"><span>{t.language}</span><select data-testid="language-select" value={prefs.language} onChange={event => update('language', event.target.value as Language)}><option value="en">English</option><option value="zh">简体中文</option></select></label>
      <label className="setting-row"><span>{t.interval}</span><select data-testid="interval-select" title={native ? (prefs.language === 'zh' ? '失败后退避重试' : 'Retry backoff on failure') : undefined} value={native ? String(prefs.refreshIntervalSeconds) : 'manual'} disabled={!native} onChange={event => {
        const seconds = Number(event.target.value)
        if (isRefreshIntervalSeconds(seconds)) update('refreshIntervalSeconds', seconds)
      }}>
        {native ? REFRESH_INTERVAL_SECONDS.map(seconds => <option key={seconds} value={String(seconds)}>{intervalLabels[seconds]}</option>) : <option value="manual">{t.manual}</option>}
      </select></label>
      <footer className="settings-footer"><div className="footer-meta"><span>{t.savedLocally}</span></div><button className="done-button" data-testid="settings-done" onClick={returnToDetail}>{t.done}</button></footer>
    </section>}
  </main>
}

function Gear() { return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="3.1"/><path d="M19.14 12.94a7.97 7.97 0 0 0 .06-.94c0-.32-.02-.64-.07-.94l2.03-1.58-1.92-3.32-2.39.96a7.28 7.28 0 0 0-1.63-.94L14.86 3H9.14l-.36 3.18a7.28 7.28 0 0 0-1.63.94l-2.39-.96-1.92 3.32 2.03 1.58a7.97 7.97 0 0 0 0 1.88l-2.03 1.58 1.92 3.32 2.39-.96c.5.38 1.05.7 1.63.94L9.14 21h5.72l.36-3.18c.58-.24 1.13-.56 1.63-.94l2.39.96 1.92-3.32-2.02-1.58Z"/></svg> }
function Close() { return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m4 4 16 16M20 4 4 20"/></svg> }
function Refresh() { return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 7v5h-5M4 17v-5h5"/><path d="M5.6 9a7 7 0 0 1 11.7-2L20 12M4 12l2.7 5a7 7 0 0 0 11.7-2"/></svg> }
function Pin() { return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m16 3 5 5-4 1-3 4-1 5-2-2-6 5 5-6-2-2 5-1 4-3z"/><path d="m8 16-3-3"/></svg> }
function Minimise() { return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14"/></svg> }
function Timer() { return <svg viewBox="0 0 24 24" aria-hidden="true" data-testid="quota-reset-timer"><circle cx="12" cy="13" r="8"/><path d="M12 9v4l2.5 1.5M12 5V3m-3 0h6"/></svg> }
function CalendarReset() { return <svg viewBox="0 0 24 24" aria-hidden="true" data-testid="quota-reset-calendar"><path d="M7 3v3m10-3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13H4V6a1 1 0 0 1 1-1Z"/><path d="M9.2 14a3.2 3.2 0 1 0 .7-2.1M9 11v3h3"/></svg> }
function WindowIcon() { return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3.5" y="4.5" width="17" height="15" rx="2"/><path d="M4 9h16M7 6.8h.1M9.5 6.8h.1"/></svg> }
function ChevronRight() { return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 5 7 7-7 7"/></svg> }
