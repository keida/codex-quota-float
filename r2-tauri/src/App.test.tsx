import { afterEach, describe, expect, it, vi } from 'vitest'
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { StrictMode } from 'react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { activityChartScale, activityXAxisTickLabels, formatActivityAxisTick, formatCompactTokenCount, formatPlanLabel, localDayEdgesUtcSeconds, planLabelTone, splitDayEdgesUtcSeconds, TOKEN_BARS, TOKEN_TOTAL, visibleQuota, type DashboardSnapshot } from './model'
import * as bridge from './bridge'

const desktopMode = vi.hoisted(() => ({ value: false }))
const dragBridge = vi.hoisted(() => ({ start: vi.fn().mockResolvedValue(undefined), finish: vi.fn().mockResolvedValue({ released: true, snapped: false }) }))
const trayBridge = vi.hoisted(() => ({ callback: null as ((action: 'open' | 'refresh') => void) | null, unlisten: vi.fn() }))

vi.mock('./bridge', () => ({
  inDesktop: () => desktopMode.value,
  readDashboardSnapshot: vi.fn().mockResolvedValue(null),
  resizeSurface: vi.fn().mockResolvedValue(undefined),
  readSystemTheme: vi.fn().mockResolvedValue(null),
  subscribeSystemTheme: vi.fn().mockResolvedValue(() => {}),
  subscribeTrayActions: vi.fn(async (callback: (action: 'open' | 'refresh') => void) => { trayBridge.callback = callback; return trayBridge.unlisten }),
  setTrayLanguage: vi.fn().mockResolvedValue(undefined),
  setNativeTheme: vi.fn().mockResolvedValue(undefined),
  startNativeDrag: dragBridge.start,
  finishNativeDrag: dragBridge.finish,
}))

afterEach(() => {
  vi.useRealTimers()
  cleanup()
  desktopMode.value = false
  vi.clearAllMocks()
  dragBridge.start.mockReset().mockResolvedValue(undefined)
  dragBridge.finish.mockReset().mockResolvedValue({ released: true, snapped: false })
  trayBridge.callback = null
  trayBridge.unlisten.mockReset()
  localStorage.clear()
  document.documentElement.removeAttribute('data-preview-backdrop')
})
const movePointerIntoDetail = () => fireEvent.pointerMove(screen.getByTestId('surface'), { clientX: 20, clientY: 20 })
function installPointerCapture(surface: HTMLElement) {
  Object.defineProperties(surface, {
    setPointerCapture: { configurable: true, value: vi.fn() },
    hasPointerCapture: { configurable: true, value: vi.fn(() => true) },
    releasePointerCapture: { configurable: true, value: vi.fn() },
  })
}
const liveSnapshot = {
  quotaStatus: 'partial' as const,
  planLabel: 'prolite',
  quotaWindows: [{ remainingPercent: 74, windowSeconds: 604800, resetsAtUnixSeconds: 2_000_000_000 }],
  quotaFetchedAtUnixSeconds: 1_800_000_000,
  retryAfterSeconds: null,
  activityStatus: 'partial' as const,
  activityTotalTokens: 1200,
  activityBuckets: [null, 120, 0, 60, null, 210, 80, 90, 140, 170, 180, 150],
  activityScannedAtUnixSeconds: 1_800_000_000,
  resetCredits: { status: 'unavailable' as const, availableCount: null, expiresAtUnixSeconds: null },
}

describe('Quota Float reference preview', () => {
  it('maps only recognized plan identifiers and preserves unknown labels', () => {
    expect(['prolite', 'PLUS', 'Pro', 'PROMAX'].map(formatPlanLabel)).toEqual(['PRO LITE', 'PLUS', 'PRO (MORE)', 'PRO (MAX)'])
    expect(formatPlanLabel('Enterprise Tier')).toBe('Enterprise Tier')
    expect(formatPlanLabel('  future-tier  ')).toBe('  future-tier  ')
    expect(formatPlanLabel('')).toBeNull()
    expect(formatPlanLabel('   ')).toBeNull()
    expect(formatPlanLabel(null)).toBeNull()
    expect(['prolite', 'pro', 'promax'].map(planLabelTone)).toEqual(['plan-pro', 'plan-pro', 'plan-pro'])
    expect(planLabelTone('plus')).toBe('plan-plus')
    expect(planLabelTone('unknown')).toBe('plan-neutral')
  })

  it('divides a local day into 12 validated bins across 23-hour and 25-hour DST days', () => {
    for (const durationHours of [23, 24, 25]) {
      const start = Date.UTC(2026, 0, 1)
      const edges = splitDayEdgesUtcSeconds(start, start + durationHours * 60 * 60 * 1000)
      expect(edges).toHaveLength(13)
      expect(edges[0]).toBe(start / 1000)
      expect(edges[12]).toBe((start + durationHours * 60 * 60 * 1000) / 1000)
      expect(edges.every((edge, index) => index === 0 || edge > edges[index - 1]!)).toBe(true)
    }
    expect(() => localDayEdgesUtcSeconds(new Date(Number.NaN))).toThrow('Invalid local-day range')
    expect(formatActivityAxisTick(0)).toBe('0')
    expect(formatActivityAxisTick(2_263_000)).toBe('2.3M')
    expect(formatActivityAxisTick(6_789_000)).toBe('6.8M')
    expect(formatActivityAxisTick(999_999)).toBe('1.0M')
    expect(formatActivityAxisTick(2_000)).toBe('2.0K')
    for (const durationHours of [23, 25]) {
      const start = Date.UTC(2026, 2, 8)
      const edges = splitDayEdgesUtcSeconds(start, start + durationHours * 60 * 60 * 1000)
      expect(activityXAxisTickLabels(edges, 'en')).toEqual([0, 3, 6, 9].map(index => new Date(edges[index]! * 1000).toLocaleTimeString('en-AU', { hour: '2-digit', minute: '2-digit', hourCycle: 'h23' })))
    }
  })

  it('formats compact token counts and rounds partial lower bounds down safely', () => {
    expect(formatCompactTokenCount(0)).toBe('0')
    expect(formatCompactTokenCount(999)).toBe('999')
    expect(formatCompactTokenCount(1_000)).toBe('1.0K')
    expect(formatCompactTokenCount(12_480)).toBe('12.5K')
    expect(formatCompactTokenCount(999_999)).toBe('1.0M')
    expect(formatCompactTokenCount(999_999, true)).toBe('999.9K')
    expect(formatCompactTokenCount(8_999_999, true)).toBe('8.9M')
    expect(formatCompactTokenCount(9_000_337)).toBe('9.0M')
  })

  it('keeps both compact plans at 96px and uses the pinned quota readings', async () => {
    expect(visibleQuota('plus').map(({ value }) => value)).toEqual([74, 38])
    expect(visibleQuota('pro').map(({ key, value }) => [key, value])).toEqual([['weekly', 38]])
    render(<App />)
    expect(bridge.readDashboardSnapshot).not.toHaveBeenCalled()
    expect(screen.getByTestId('widget')).toHaveTextContent('5h74%wk38%')
    expect(screen.queryByTestId('compact-plan')).not.toBeInTheDocument()
    expect(screen.getByTestId('compact-five-hour-arc')).toHaveAttribute('stroke-dashoffset', '26')
    expect(screen.getByTestId('compact-weekly-arc')).toHaveAttribute('stroke-dashoffset', '62')
    expect(screen.getByTestId('compact-weekly-reading')).toHaveClass('compact-reading-weekly')
    for (const arc of [screen.getByTestId('compact-five-hour-arc'), screen.getByTestId('compact-weekly-arc')]) {
      expect(arc).toHaveAttribute('stroke-linecap', 'round')
      expect(arc).toHaveAttribute('transform', 'rotate(-90 50 50)')
    }
    await waitFor(() => expect(bridge.resizeSurface).toHaveBeenLastCalledWith('widget'))

    fireEvent.click(screen.getByTestId('widget'))
    movePointerIntoDetail()
    expect(within(screen.getByTestId('quota-fiveHour')).getByTestId('quota-reset-timer')).toBeInTheDocument()
    expect(within(screen.getByTestId('quota-weekly')).getByTestId('quota-reset-calendar')).toBeInTheDocument()
    fireEvent.click(screen.getByTestId('plan-pro'))
    expect(screen.getByTestId('quota-weekly')).toHaveTextContent('38%')
    expect(screen.queryByTestId('quota-fiveHour')).not.toBeInTheDocument()
    await waitFor(() => expect(bridge.resizeSurface).toHaveBeenLastCalledWith('detail'))

    vi.useFakeTimers()
    const surface = screen.getByTestId('surface')
    fireEvent.mouseLeave(surface)
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.getByTestId('widget')).toHaveTextContent('38%wk')
    expect(screen.getByTestId('compact-weekly-arc')).toHaveAttribute('stroke-dashoffset', '62')
  })

  it('collapses a hover-only detail when the pointer leaves', async () => {
    vi.useFakeTimers()
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    movePointerIntoDetail()
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    fireEvent.pointerLeave(screen.getByTestId('surface'))
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('opens settings without losing detail, then returns focus to the settings control', async () => {
    const user = userEvent.setup()
    render(<App />)
    fireEvent.click(screen.getByTestId('widget'))
    movePointerIntoDetail()
    await user.click(screen.getByTestId('settings-open'))
    expect(screen.getByTestId('settings-panel')).toBeInTheDocument()
    await waitFor(() => expect(bridge.resizeSurface).toHaveBeenLastCalledWith('settings'))
    fireEvent.mouseLeave(screen.getByTestId('surface'))
    expect(screen.getByTestId('settings-panel')).toBeInTheDocument()
    await user.click(screen.getByTestId('settings-done'))
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    expect(screen.getByTestId('settings-open')).toHaveFocus()

    vi.useFakeTimers()
    const surface = screen.getByTestId('surface')
    vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 340, 480))
    fireEvent.pointerLeave(surface)
    fireEvent.mouseLeave(surface)
    fireEvent.pointerMove(window, { clientX: 120, clientY: 500 })
    fireEvent.pointerLeave(surface, { clientX: 120, clientY: 500 })
    await act(async () => { vi.advanceTimersByTime(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('collapses click-open detail after the pointer leaves', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    render(<App />)
    const widget = screen.getByTestId('widget')
    fireEvent.pointerEnter(widget, { pointerId: 1, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    const surface = screen.getByTestId('surface')
    installPointerCapture(surface)
    fireEvent.pointerDown(surface, { pointerId: 1, pointerType: 'mouse', isPrimary: true, button: 0, clientX: 8, clientY: 8 })
    fireEvent.pointerUp(surface, { pointerId: 1, pointerType: 'mouse', isPrimary: true, button: 0, clientX: 8, clientY: 8 })
    fireEvent.click(surface, { clientX: 8, clientY: 8, detail: 1 })
    fireEvent.pointerLeave(surface, { pointerId: 1, pointerType: 'mouse', clientX: 400, clientY: 500 })
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })

    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
  })

  it('does not pin detail when an ordinary detail click follows pointer movement', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 2, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    const surface = screen.getByTestId('surface')
    installPointerCapture(surface)
    fireEvent.pointerMove(surface, { pointerId: 2, pointerType: 'mouse', clientX: 12.5, clientY: 8 })
    fireEvent.pointerDown(surface, { pointerId: 2, pointerType: 'mouse', isPrimary: true, button: 0, clientX: 12.5, clientY: 8 })
    fireEvent.pointerUp(surface, { pointerId: 2, pointerType: 'mouse', isPrimary: true, button: 0, clientX: 12.5, clientY: 8 })
    fireEvent.click(surface, { clientX: 12.5, clientY: 8, detail: 1 })
    fireEvent.pointerLeave(surface, { pointerId: 2, pointerType: 'mouse', clientX: 400, clientY: 500 })
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })

    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('suppresses the default context menu on the app surface', () => {
    render(<App />)
    const contextMenu = new MouseEvent('contextmenu', { bubbles: true, cancelable: true })
    fireEvent(screen.getByTestId('surface'), contextMenu)
    expect(contextMenu.defaultPrevented).toBe(true)
  })

  it.each([
    { name: 'confirmed docked release', result: { released: true, snapped: true }, from: 'detail', expected: 'widget' },
    { name: 'free release', result: { released: true, snapped: false }, from: 'detail', expected: 'detail' },
    { name: 'unconfirmed release despite snap bit', result: { released: false, snapped: true }, from: 'detail', expected: 'detail' },
    { name: 'docked release from Settings', result: { released: true, snapped: true }, from: 'settings', expected: 'settings-panel' },
  ])('handles $name without dismissing the wrong surface', async ({ result, from, expected }) => {
    desktopMode.value = true
    dragBridge.finish.mockResolvedValueOnce(result)
    render(<App />)
    fireEvent.click(screen.getByTestId('widget'))
    if (from === 'settings') fireEvent.click(screen.getByTestId('settings-open'))
    expect(screen.getByTestId(from === 'settings' ? 'settings-panel' : 'detail')).toBeInTheDocument()

    const surface = screen.getByTestId('surface')
    installPointerCapture(surface)
    fireEvent.pointerDown(surface, { pointerId: 19, isPrimary: true, button: 0, clientX: 10, clientY: 10 })
    fireEvent.pointerMove(surface, { pointerId: 19, isPrimary: true, clientX: 16, clientY: 10 })
    fireEvent.pointerUp(surface, { pointerId: 19, isPrimary: true, button: 0, clientX: 16, clientY: 10 })
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    expect(screen.getByTestId(expected)).toBeInTheDocument()
    if (expected === 'widget') expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
    if (result.released === false) expect(surface).toHaveAttribute('data-dragging', 'true')
  })

  it('lets a mouse user collapse click-open detail back to the compact widget', () => {
    render(<App />)
    fireEvent.click(screen.getByTestId('widget'))
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    fireEvent.click(screen.getByTestId('collapse-to-widget'))
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
  })

  it.each(['mouse Minimise', 'Escape'] as const)('stays compact through resize re-entry after %s, then expands after a real pointer departure and re-entry', async collapseRoute => {
    vi.useFakeTimers()
    desktopMode.value = true
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 4, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    const surface = screen.getByTestId('surface')
    vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 96, 96))
    fireEvent.pointerMove(surface, { pointerId: 4, pointerType: 'mouse', clientX: 20, clientY: 20 })
    if (collapseRoute === 'mouse Minimise') {
      fireEvent.click(screen.getByTestId('collapse-to-widget'), { detail: 1, clientX: 20, clientY: 20 })
    } else {
      fireEvent.keyDown(window, { key: 'Escape' })
    }
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()

    fireEvent.pointerMove(window, { pointerId: 4, pointerType: 'mouse', clientX: 24, clientY: 24 })
    fireEvent.pointerLeave(surface, { pointerId: 4, pointerType: 'mouse', clientX: 24, clientY: 24 })
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 4, pointerType: 'mouse', clientX: 24, clientY: 24 })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()

    fireEvent.pointerLeave(surface, { pointerId: 4, pointerType: 'mouse', clientX: 500, clientY: 500 })
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()

    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 4, pointerType: 'mouse', clientX: 20, clientY: 20 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()
  })

  it('lets keyboard activation collapse detail when the pointer origin is retained', async () => {
    desktopMode.value = true
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 3, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    screen.getByTestId('collapse-to-widget').focus()
    await userEvent.setup().keyboard('{Enter}')

    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
  })

  it('moves keyboard focus into detail and returns to the compact widget on Escape', async () => {
    render(<App />)
    fireEvent.focus(screen.getByTestId('widget'))
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    expect(screen.getByTestId('settings-open')).toHaveFocus()
    fireEvent.keyDown(window, { key: 'Escape' })
    await waitFor(() => expect(screen.getByTestId('widget')).toHaveFocus())
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
  })

  it('preserves a click without movement and excludes toolbar controls from native dragging', async () => {
    desktopMode.value = true
    vi.useFakeTimers()
    render(<App />)
    const surface = screen.getByTestId('surface')
    installPointerCapture(surface)
    const widget = screen.getByTestId('widget')
    fireEvent.pointerDown(widget, { pointerId: 7, isPrimary: true, button: 0, clientX: 5, clientY: 5 })
    fireEvent.pointerUp(surface, { pointerId: 7, isPrimary: true, button: 0, clientX: 5, clientY: 5 })
    expect(dragBridge.start).not.toHaveBeenCalled()
    expect(dragBridge.finish).not.toHaveBeenCalled()
    fireEvent.click(widget)
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    fireEvent.pointerLeave(surface, { pointerId: 7, pointerType: 'mouse', clientX: 400, clientY: 500 })
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
  })

  it('keeps the drag latch through pointer cancellation until the native bridge confirms release', async () => {
    desktopMode.value = true
    vi.useFakeTimers()
    dragBridge.finish.mockReset().mockResolvedValueOnce({ released: false, snapped: false }).mockResolvedValueOnce({ released: true, snapped: false })
    render(<App />)
    const surface = screen.getByTestId('surface')
    installPointerCapture(surface)
    fireEvent.pointerDown(screen.getByTestId('widget'), { pointerId: 9, isPrimary: true, button: 0, clientX: 10, clientY: 10 })
    fireEvent.pointerMove(surface, { pointerId: 9, isPrimary: true, clientX: 16, clientY: 10 })
    expect(dragBridge.start).toHaveBeenCalledTimes(1)
    expect(surface).toHaveAttribute('data-dragging', 'true')

    fireEvent.pointerCancel(surface, { pointerId: 9, isPrimary: true })
    expect(dragBridge.finish).not.toHaveBeenCalled()
    await act(async () => { await vi.advanceTimersByTimeAsync(125) })
    expect(dragBridge.finish).toHaveBeenCalledTimes(1)
    expect(surface).toHaveAttribute('data-dragging', 'true')
    await act(async () => { await vi.advanceTimersByTimeAsync(125) })
    expect(dragBridge.finish).toHaveBeenCalledTimes(2)
    expect(surface).toHaveAttribute('data-dragging', 'false')
  })

  it('plots all 12 buckets on the 0–3000 scale and keeps the peak label on the true peak', () => {
    expect(TOKEN_TOTAL).toBe(12480)
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    expect(screen.getByTestId('detail')).toHaveTextContent(/12\.5K\s+tokens/)
    expect(screen.getByText('This device only')).toBeInTheDocument()
    expect(screen.getByTestId('activity-total')).toHaveTextContent(/12\.5K\s+tokens/)
    expect(screen.getByTestId('device-label')).toBeInTheDocument()
    expect(screen.getByText('0')).toBeInTheDocument()
    expect(screen.getByText('3.0K')).toBeInTheDocument()
    expect(screen.getByTestId('axis-tick-0')).toHaveTextContent(activityXAxisTickLabels(localDayEdgesUtcSeconds(), 'en')[0]!)
    expect(screen.getByTestId('axis-tick-3')).toHaveTextContent(activityXAxisTickLabels(localDayEdgesUtcSeconds(), 'en')[3]!)
    expect([0, 1, 2, 3].map(index => screen.getByTestId(`axis-tick-${index}`).getAttribute('data-tick-position'))).toEqual(['0%', '25%', '50%', '75%'])
    expect(screen.getByTestId('axis-tick-0').getAttribute('style')).toContain('--tick-position: 0%')
    expect(screen.getByTestId('axis-tick-3').getAttribute('style')).toContain('--tick-position: 75%')
    const bars = screen.getAllByTestId('token-bar')
    expect(bars).toHaveLength(12)
    expect(bars.map(bar => Number(bar.getAttribute('data-value')))).toEqual(TOKEN_BARS)
    expect(bars[0]).toHaveStyle({ '--bar-height': '6%' })
    expect(bars[6]).toHaveStyle({ '--bar-height': '82%' })
    expect(bars[6]).toHaveClass('bar-blue')
    expect(screen.getByTestId('peak-label')).toHaveTextContent('2.5K')
    expect(screen.getByTestId('peak-label')).toHaveAttribute('aria-label', 'Peak: 2,460 tokens')
    expect(screen.getByTestId('peak-label').parentElement).toBe(bars[6])
    expect(screen.getByTestId('reset-opportunities')).toHaveTextContent('2 reset opportunities')
    expect(screen.getByTestId('reset-opportunities')).toHaveTextContent('Expires 31 Oct 2026')
  })

  it('offers three local theme choices, honest disabled settings, and a sample-only refresh', async () => {
    const user = userEvent.setup()
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    movePointerIntoDetail()
    await user.click(screen.getByTestId('settings-open'))
    expect(screen.getByTestId('theme-system')).toHaveAttribute('aria-pressed', 'true')
    await user.click(screen.getByTestId('theme-dark'))
    expect(screen.getByTestId('theme-dark')).toHaveAttribute('aria-pressed', 'true')
    expect(bridge.setNativeTheme).toHaveBeenCalledWith('dark')
    expect(screen.getByTestId('interval-select')).toBeDisabled()
    expect(screen.queryByTestId('follow-toggle')).not.toBeInTheDocument()
    expect(screen.queryByTestId('alerts-toggle')).not.toBeInTheDocument()
    expect(screen.getByText('Saved locally')).toBeInTheDocument()
    expect(screen.queryByText('About')).not.toBeInTheDocument()
    await user.selectOptions(screen.getByTestId('language-select'), 'zh')
    await waitFor(() => expect(screen.getByRole('heading', { name: '设置' })).toBeInTheDocument())
    expect(screen.queryByTestId('follow-toggle')).not.toBeInTheDocument()
    expect(document.documentElement.lang).toBe('zh-CN')
    expect(JSON.parse(localStorage.getItem('quota-float-r2-preview.preferences') ?? '{}')).toMatchObject({ theme: 'dark', language: 'zh' })
    movePointerIntoDetail()
    await user.click(screen.getByTestId('settings-done'))
    expect(screen.getByTestId('detail')).toHaveTextContent('仅此设备')
    expect(screen.getByTestId('detail')).toHaveTextContent('每2小时 tokens')
    await user.click(screen.getByTestId('refresh-sample'))
    expect(bridge.readDashboardSnapshot).not.toHaveBeenCalled()
    expect(screen.getByText('未连接')).toBeInTheDocument()
    expect(screen.getByText(/样本已在本地刷新/)).toBeInTheDocument()
    expect(screen.getByText('今天').querySelector('svg')).toBeNull()
  })

  it('validates and persists only supported native refresh intervals', () => {
    desktopMode.value = true
    localStorage.setItem('quota-float-r2-preview.preferences', JSON.stringify({ refreshIntervalSeconds: 90 }))
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    fireEvent.click(screen.getByTestId('settings-open'))

    const interval = screen.getByTestId('interval-select') as HTMLSelectElement
    expect(interval).toHaveValue('60')
    fireEvent.change(interval, { target: { value: '300' } })
    expect(interval).toHaveValue('300')
    expect(JSON.parse(localStorage.getItem('quota-float-r2-preview.preferences') ?? '{}')).toMatchObject({ refreshIntervalSeconds: 300 })
    expect(Array.from(interval.options).map(option => option.value)).toEqual(['60', '120', '300'])
  })

  it('rebases one pending healthy poll to the latest selected interval', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue(liveSnapshot)
    render(<App />)
    await act(async () => { await Promise.resolve(); await Promise.resolve() })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    fireEvent.click(screen.getByTestId('settings-open'))
    const interval = screen.getByTestId('interval-select')
    fireEvent.change(interval, { target: { value: '120' } })
    fireEvent.change(interval, { target: { value: '300' } })

    await act(async () => { await vi.advanceTimersByTimeAsync(299_999) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
    await act(async () => { await vi.advanceTimersByTimeAsync(1) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2)
  })

  it('uses the selected interval when the initial healthy request is still in flight', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    let resolveInitial!: (value: DashboardSnapshot) => void
    vi.mocked(bridge.readDashboardSnapshot).mockImplementationOnce(() => new Promise(resolve => { resolveInitial = resolve }))
      .mockResolvedValue(liveSnapshot)
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    fireEvent.click(screen.getByTestId('settings-open'))
    fireEvent.change(screen.getByTestId('interval-select'), { target: { value: '120' } })
    resolveInitial(liveSnapshot)
    await act(async () => { await Promise.resolve(); await Promise.resolve() })

    await act(async () => { await vi.advanceTimersByTimeAsync(119_999) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
    await act(async () => { await vi.advanceTimersByTimeAsync(1) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2)
  })

  it('keeps Retry-After as a floor when the selected interval is shorter', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot)
      .mockResolvedValueOnce({ ...liveSnapshot, quotaStatus: 'rateLimited', retryAfterSeconds: 180 })
      .mockResolvedValue(liveSnapshot)
    render(<App />)
    await act(async () => { await Promise.resolve(); await Promise.resolve() })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    fireEvent.click(screen.getByTestId('settings-open'))
    fireEvent.change(screen.getByTestId('interval-select'), { target: { value: '120' } })

    await act(async () => { await vi.advanceTimersByTimeAsync(179_999) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
    await act(async () => { await vi.advanceTimersByTimeAsync(1) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2)
  })

  it('resets automatic failure backoff after a successful manual refresh', async () => {
    vi.useFakeTimers()
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot)
      .mockResolvedValueOnce({ ...liveSnapshot, quotaStatus: 'unavailable' })
      .mockResolvedValueOnce(liveSnapshot)
      .mockResolvedValueOnce({ ...liveSnapshot, quotaStatus: 'unavailable' })
      .mockResolvedValue(liveSnapshot)
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    await act(async () => { await Promise.resolve(); await Promise.resolve() })
    const refresh = screen.getByTestId('refresh-live')
    expect(refresh).toBeEnabled()
    fireEvent.click(refresh)
    await act(async () => { await Promise.resolve(); await Promise.resolve() })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2)

    await act(async () => { await vi.advanceTimersByTimeAsync(59_999) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2)
    await act(async () => { await vi.advanceTimersByTimeAsync(1) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(3)
    await act(async () => { await vi.advanceTimersByTimeAsync(4_999) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(3)
    await act(async () => { await vi.advanceTimersByTimeAsync(1) })
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(4)
  })

  it.each([
    { name: 'fully available quota', result: { ...liveSnapshot, quotaStatus: 'available' as const, activityStatus: 'available' as const, resetCredits: { status: 'available' as const, availableCount: 0, expiresAtUnixSeconds: null } }, expected: 'Quota updated' },
    { name: 'partial activity', result: { ...liveSnapshot, quotaStatus: 'available' as const }, expected: 'Quota updated' },
    { name: 'stale reset-credit data', result: { ...liveSnapshot, quotaStatus: 'available' as const, resetCredits: { status: 'stale' as const, availableCount: 2, expiresAtUnixSeconds: 2_000_000_000 } }, expected: 'Quota updated' },
    { name: 'unavailable activity', result: { ...liveSnapshot, quotaStatus: 'available' as const, activityStatus: 'unavailable' as const }, expected: 'Quota updated' },
    { name: 'partial quota', result: { ...liveSnapshot, quotaStatus: 'partial' as const }, expected: 'Quota partially updated' },
    { name: 'stale quota', result: { ...liveSnapshot, quotaStatus: 'stale' as const }, expected: 'Refresh incomplete · showing stale data' },
    { name: 'signed-out quota', result: { ...liveSnapshot, quotaStatus: 'signedOut' as const }, expected: 'Refresh incomplete · data unavailable' },
  ])('reports manual refresh outcome honestly for $name', async ({ result, expected }) => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot)
      .mockResolvedValueOnce({ ...liveSnapshot, quotaStatus: 'available' })
      .mockResolvedValueOnce(result)
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(screen.getByTestId('refresh-live')).toBeEnabled())
    expect(screen.queryByTestId('refresh-feedback')).not.toBeInTheDocument()

    fireEvent.click(screen.getByTestId('refresh-live'))
    expect(await screen.findByTestId('refresh-feedback')).toHaveTextContent(expected)
    expect(screen.getByTestId('refresh-feedback')).toHaveAttribute('role', 'status')
  })

  it('reports a rejected manual refresh as a failure', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot)
      .mockResolvedValueOnce({ ...liveSnapshot, quotaStatus: 'available' })
      .mockRejectedValueOnce(new Error('private backend error'))
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(screen.getByTestId('refresh-live')).toBeEnabled())

    fireEvent.click(screen.getByTestId('refresh-live'))
    expect(await screen.findByTestId('refresh-feedback')).toHaveTextContent('Refresh failed')
  })

  it('opens tray details and joins an active read for refresh without duplicating it', async () => {
    desktopMode.value = true
    let resolveInitial!: (value: DashboardSnapshot) => void
    vi.mocked(bridge.readDashboardSnapshot).mockImplementation(() => new Promise(resolve => { resolveInitial = resolve }))
    render(<App />)
    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(trayBridge.callback).not.toBeNull())

    await act(async () => { trayBridge.callback?.('refresh') })
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    expect(screen.getByTestId('refresh-feedback')).toHaveTextContent('Refreshing…')
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)

    fireEvent.pointerLeave(screen.getByTestId('surface'))
    await act(async () => { await new Promise(resolve => window.setTimeout(resolve, 140)) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    await act(async () => { resolveInitial({ ...liveSnapshot, quotaStatus: 'available' }) })
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 6, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(await screen.findByTestId('refresh-feedback')).toHaveTextContent('Quota updated')
    expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1)
  })

  it('restores compact from tray Open when the pointer is outside, then expands on its first genuine hover', async () => {
    desktopMode.value = true
    render(<App />)
    await waitFor(() => expect(trayBridge.callback).not.toBeNull())
    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 3, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()

    await act(async () => { trayBridge.callback?.('open') })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.getByTestId('widget')).toHaveFocus()
    expect(screen.queryByTestId('detail')).not.toBeInTheDocument()
    fireEvent.focus(window)
    expect(screen.getByTestId('widget')).toBeInTheDocument()

    fireEvent.pointerEnter(screen.getByTestId('widget'), { pointerId: 3, pointerType: 'mouse', clientX: 8, clientY: 8 })
    expect(screen.getByTestId('detail')).toBeInTheDocument()
    vi.useFakeTimers()
    fireEvent.pointerLeave(screen.getByTestId('surface'), { pointerId: 3, pointerType: 'mouse', clientX: 400, clientY: 500 })
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('keeps an already compact widget compact when tray Open only shows and focuses the window', async () => {
    desktopMode.value = true
    render(<App />)
    await waitFor(() => expect(trayBridge.callback).not.toBeNull())
    fireEvent.focus(window)
    await act(async () => { trayBridge.callback?.('open') })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
    expect(screen.getByTestId('widget')).not.toHaveFocus()
    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('keeps visible Settings through tray Open, pointer leave, blur, and Refresh until Done or Escape', async () => {
    desktopMode.value = true
    render(<App />)
    await waitFor(() => expect(trayBridge.callback).not.toBeNull())
    vi.useFakeTimers()
    fireEvent.click(screen.getByTestId('widget'))
    fireEvent.click(screen.getByTestId('settings-open'))
    expect(screen.getByTestId('settings-panel')).toBeInTheDocument()

    await act(async () => { trayBridge.callback?.('open') })
    fireEvent.pointerLeave(screen.getByTestId('surface'))
    fireEvent.blur(window)
    await act(async () => { await vi.advanceTimersByTimeAsync(200) })
    expect(screen.getByTestId('settings-panel')).toBeInTheDocument()

    await act(async () => { trayBridge.callback?.('refresh') })
    expect(screen.getByTestId('settings-panel')).toBeInTheDocument()
    fireEvent.keyDown(window, { key: 'Escape' })
    expect(screen.getByTestId('widget')).toBeInTheDocument()
  })

  it('sends the saved language preference to the native tray', async () => {
    desktopMode.value = true
    localStorage.setItem('quota-float-r2-preview.preferences', JSON.stringify({ language: 'zh' }))
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue(liveSnapshot)
    render(<App />)
    await waitFor(() => expect(bridge.setTrayLanguage).toHaveBeenCalledWith('zh'))
  })

  it('registers only the live tray listener when StrictMode replays effects', async () => {
    desktopMode.value = true
    render(<StrictMode><App /></StrictMode>)
    await waitFor(() => expect(bridge.subscribeTrayActions).toHaveBeenCalledTimes(1))
  })

  it('announces a manual refresh in progress without changing automatic polling feedback', async () => {
    desktopMode.value = true
    let resolveManual!: (value: DashboardSnapshot) => void
    const completeSnapshot = { ...liveSnapshot, quotaStatus: 'available' as const, activityStatus: 'available' as const, resetCredits: { status: 'available' as const, availableCount: 0, expiresAtUnixSeconds: null } }
    vi.mocked(bridge.readDashboardSnapshot)
      .mockResolvedValueOnce(completeSnapshot)
      .mockImplementationOnce(() => new Promise(resolve => { resolveManual = resolve }))
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(screen.getByTestId('refresh-live')).toBeEnabled())

    fireEvent.click(screen.getByTestId('refresh-live'))
    expect(screen.getByTestId('quota-status')).toHaveTextContent('Refreshing…')
    expect(screen.getByTestId('refresh-live')).toHaveAccessibleName('Refreshing…')
    expect(screen.getByTestId('refresh-live')).toBeDisabled()
    resolveManual(completeSnapshot)
    expect(await screen.findByTestId('refresh-feedback')).toHaveTextContent('Quota updated')
  })

  it('renders only returned native windows and marks partial local activity without filling unknown buckets', async () => {
    desktopMode.value = true
    let resolveInitial!: (value: typeof liveSnapshot) => void
    vi.mocked(bridge.readDashboardSnapshot)
      .mockImplementationOnce(() => new Promise(resolve => { resolveInitial = resolve }))
      .mockResolvedValue(liveSnapshot)
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    movePointerIntoDetail()

    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(1))
    expect(screen.getByTestId('refresh-live')).toBeDisabled()
    resolveInitial(liveSnapshot)

    const dayEdges = vi.mocked(bridge.readDashboardSnapshot).mock.calls[0]?.[0] ?? []
    expect(dayEdges).toHaveLength(13)
    expect(dayEdges.every((edge, index) => index === 0 || (dayEdges[index - 1] !== undefined && edge > dayEdges[index - 1]!))).toBe(true)
    expect(await screen.findByTestId('plan-label')).toHaveTextContent('PRO LITE')
    expect(screen.getByTestId('quota-window-label')).toHaveTextContent('Weekly')
    expect(screen.queryByTestId('quota-fiveHour')).not.toBeInTheDocument()
    expect(screen.queryByTestId('plan-plus')).not.toBeInTheDocument()
    expect(screen.getByTestId('quota-status')).toHaveTextContent('Partial')
    expect(screen.getByTestId('activity-total')).toHaveTextContent('≥1.2K tokens')
    expect(screen.getByTestId('activity-total').querySelector('strong')).toHaveAttribute('aria-label', '≥1,200 tokens')
    expect(screen.getByTestId('plan-label')).toHaveAttribute('title', 'prolite · PRO LITE')
    expect(screen.getByTestId('plan-label')).toHaveAttribute('aria-label', 'Plan: PRO LITE (identifier: prolite)')
    expect(screen.getByTestId('device-label')).toHaveTextContent('This device only')
    expect(screen.getAllByTestId('token-bar')).toHaveLength(12)
    expect(screen.getAllByTestId('token-bar').filter(bar => bar.getAttribute('data-value') === 'unknown')).toHaveLength(2)
    expect(screen.getByTestId('weekly-reset-panel')).toHaveTextContent('Resets in')
    expect(screen.getByTestId('weekly-reset-panel')).not.toHaveTextContent(/reset opportunities|Expires/)
    expect(screen.getByTestId('credit-reset-status')).toHaveTextContent('Credit reset details not read')
    expect(screen.getByTestId('chart-caption')).toHaveTextContent('Partial data')
    expect(screen.queryByText('?')).not.toBeInTheDocument()

    await waitFor(() => expect(screen.getByTestId('refresh-live')).toBeEnabled())
    await userEvent.setup().click(screen.getByTestId('refresh-live'))
    await waitFor(() => expect(bridge.readDashboardSnapshot).toHaveBeenCalledTimes(2))
    expect(vi.mocked(bridge.readDashboardSnapshot).mock.calls[1]?.[1]).toBe(true)
  })

  it('distinguishes a valid zero reset-credit count from unavailable and stale data', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({
      ...liveSnapshot,
      resetCredits: { status: 'available', availableCount: 0, expiresAtUnixSeconds: null },
    })
    const first = render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    expect(await screen.findByTestId('credit-reset-status')).toHaveTextContent('Reset credits available: 0')
    first.unmount()

    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({
      ...liveSnapshot,
      resetCredits: { status: 'stale', availableCount: 2, expiresAtUnixSeconds: 2_000_000_000 },
    })
    render(<App />)
    fireEvent.pointerEnter(screen.getByTestId('widget'))
    expect(await screen.findByTestId('credit-reset-status')).toHaveTextContent('Reset credit data is stale')
    expect(screen.getByTestId('credit-reset-status')).not.toHaveTextContent('2')
  })

  it('uses the weekly compact treatment from the returned window duration without changing the compact widget', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue(liveSnapshot)
    render(<App />)
    expect(await screen.findByTestId('compact-weekly-value')).toHaveTextContent('74%')
    expect(screen.getByTestId('widget')).toHaveTextContent('74%wk')
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    expect(await screen.findByTestId('plan-label')).toHaveTextContent('PRO LITE')
    expect(screen.getByTestId('weekly-reset-panel')).toBeInTheDocument()
    expect(screen.queryByTestId('plan-pro')).not.toBeInTheDocument()
  })

  it('keeps unknown native plan identifiers readable and never coerces them to a known tier', async () => {
    desktopMode.value = true
    localStorage.setItem('quota-float-r2-preview.preferences', JSON.stringify({ language: 'zh' }))
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({ ...liveSnapshot, planLabel: 'future-enterprise-tier-with-a-long-name' })
    const first = render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    const label = await screen.findByTestId('plan-label')
    expect(label).toHaveTextContent('future-enterprise-tier-with-a-long-name')
    expect(label).toHaveAttribute('title', 'future-enterprise-tier-with-a-long-name')
    expect(label).toHaveAttribute('aria-label', '套餐：future-enterprise-tier-with-a-long-name（标识符：future-enterprise-tier-with-a-long-name）')
    expect(label).toHaveClass('plan-neutral')
    expect(screen.queryByTestId('plan-plus')).not.toBeInTheDocument()
    expect(screen.queryByTestId('plan-pro')).not.toBeInTheDocument()
    first.unmount()

    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({ ...liveSnapshot, planLabel: '   ' })
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    expect(await screen.findByTestId('plan-label')).toHaveTextContent('未知')
  })

  it('uses the shared short labels for native two-window compact values without changing provider identity', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({
      ...liveSnapshot,
      quotaWindows: [
        { remainingPercent: 74, windowSeconds: 18_000, resetsAtUnixSeconds: 2_000_000_000 },
        { remainingPercent: 38, windowSeconds: 604_800, resetsAtUnixSeconds: 2_000_000_000 },
      ],
    })
    render(<App />)
    expect(await screen.findByTestId('compact-five-hour-reading')).toHaveTextContent('5h74%')
    expect(screen.getByTestId('compact-weekly-reading')).toHaveTextContent('wk38%')
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    expect(await screen.findByTestId('plan-label')).toHaveTextContent('PRO LITE')
    expect(screen.getAllByTestId('quota-window-label').map(label => label.textContent)).toEqual(['5-hour remaining', 'Weekly remaining'])
  })

  it('shows native polling cadence without unimplemented settings', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue(liveSnapshot)
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    movePointerIntoDetail()
    await userEvent.setup().click(await screen.findByTestId('settings-open'))
    expect(screen.getByTestId('interval-select')).toHaveValue('60')
    expect(screen.getByTestId('interval-select')).toHaveTextContent('60 sec')
    expect(screen.queryByTestId('follow-toggle')).not.toBeInTheDocument()
    expect(screen.queryByTestId('alerts-toggle')).not.toBeInTheDocument()
    expect(screen.queryByText('About')).not.toBeInTheDocument()
    expect(screen.getByText('Saved locally')).toBeInTheDocument()
  })

  it('renders zero activity without a false peak and scales extreme first/last peaks with full values', async () => {
    desktopMode.value = true
    const base = { ...liveSnapshot, quotaStatus: 'available' as const, activityStatus: 'available' as const }
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({ ...base, activityTotalTokens: 0, activityBuckets: Array<number | null>(12).fill(0) })
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))
    await waitFor(() => expect(screen.getByTestId('activity-total')).toHaveTextContent('0 tokens'))
    expect(screen.queryByTestId('peak-label')).not.toBeInTheDocument()
    expect(screen.getAllByTestId('token-bar').every(bar => bar.getAttribute('data-value') === '0')).toBe(true)

    for (const peakIndex of [0, 5, 11]) {
      cleanup()
      const buckets = Array<number | null>(12).fill(0)
      buckets[peakIndex] = 6_787_307
      vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({ ...base, activityTotalTokens: 6_787_307, activityBuckets: buckets })
      render(<App />)
      fireEvent.mouseEnter(screen.getByTestId('widget'))
      const peak = await screen.findByTestId('peak-label')
      expect(peak).toHaveTextContent('6.8M')
      expect(peak.parentElement).toHaveAttribute('data-peak', 'true')
      expect(peak).toHaveAttribute('title', 'Peak: 6,787,307 tokens')
      expect(screen.getByTestId('peak-label')).toHaveAttribute('aria-label', 'Peak: 6,787,307 tokens')
      expect(screen.getByTestId('token-chart')).toHaveAttribute('aria-label', expect.stringContaining('6,787,307 tokens'))
      expect(screen.getByTestId('token-plot')).toHaveAttribute('data-chart-scale', '7500000')
      expect(screen.getAllByTestId('token-bar')[peakIndex]).toHaveAttribute('data-peak-edge', peakIndex === 0 ? 'start' : peakIndex === 11 ? 'end' : 'middle')
      expect(screen.getByTestId('token-chart')).toHaveTextContent('6.8M')
      expect(screen.getByTestId('token-chart')).toHaveTextContent('2.5M')
    }
  })

  it('chooses finite nice-number chart scales above every valid peak', () => {
    expect(activityChartScale(0)).toBe(3_000)
    expect(activityChartScale(13_100_000)).toBe(15_000_000)
    expect(activityChartScale(6_787_307)).toBe(7_500_000)
    for (const peak of [1, 999_999, 13_100_000, Number.MAX_SAFE_INTEGER]) {
      const scale = activityChartScale(peak)
      expect(Number.isFinite(scale)).toBe(true)
      expect(scale).toBeGreaterThanOrEqual(peak)
    }
  })

  it('keeps partial totals as safe lower bounds while exposing the exact raw count', async () => {
    desktopMode.value = true
    const count = 8_999_999
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({
      ...liveSnapshot,
      activityTotalTokens: count,
      activityBuckets: [count, null, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    })
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))

    const total = await screen.findByTestId('activity-total')
    const reading = total.querySelector('strong')
    expect(reading).toHaveTextContent('≥8.9M tokens')
    expect(reading).toHaveAttribute('aria-label', '≥8,999,999 tokens')
    expect(reading).toHaveAttribute('title', '≥8,999,999 tokens')
    expect(screen.getByTestId('peak-label')).toHaveTextContent('9.0M')
    expect(screen.getByTestId('peak-label')).toHaveAttribute('title', 'Peak: 8,999,999 tokens')
  })

  it('keeps cached quota visible when stale and marks it as stale rather than live', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockResolvedValue({ ...liveSnapshot, quotaStatus: 'stale' })
    render(<App />)
    expect(await screen.findByTestId('compact-stale-cue')).toBeInTheDocument()
    expect(screen.getByTestId('widget')).toHaveAttribute('aria-label', expect.stringContaining('Stale'))
    fireEvent.mouseEnter(screen.getByTestId('widget'))

    expect(await screen.findByTestId('quota-window-label')).toHaveTextContent('Weekly')
    expect(screen.getByTestId('quota-status')).toHaveTextContent('Stale')
    expect(screen.getByTestId('quota-status-dot')).not.toHaveClass('status-dot-live')
  })

  it('does not replace a failed native read with browser sample values', async () => {
    desktopMode.value = true
    vi.mocked(bridge.readDashboardSnapshot).mockRejectedValue(new Error('private backend error'))
    render(<App />)
    fireEvent.mouseEnter(screen.getByTestId('widget'))

    expect(await screen.findByTestId('quota-status')).toHaveTextContent('Refresh failed')
    expect(screen.getByTestId('activity-total')).toHaveTextContent('Unknown')
    expect(screen.queryByTestId('plan-plus')).not.toBeInTheDocument()
    expect(screen.queryByText('Sample data')).not.toBeInTheDocument()
    expect(screen.getByTestId('reset-opportunities')).toHaveTextContent('Credit reset details not read')
    expect(screen.getAllByTestId('token-bar').every(bar => bar.getAttribute('data-value') === 'unknown')).toBe(true)
    expect(screen.getByTestId('chart-empty-state')).toHaveTextContent('Activity data unavailable')
    expect(screen.queryByText('?')).not.toBeInTheDocument()
  })
})
