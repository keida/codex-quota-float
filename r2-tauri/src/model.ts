export type Plan = 'plus' | 'pro'
export type Language = 'en' | 'zh'
export type ThemeChoice = 'system' | 'light' | 'dark'
export type View = 'widget' | 'detail' | 'settings'

export type QuotaStatus = 'available' | 'partial' | 'stale' | 'unavailable' | 'signedOut' | 'rateLimited'
export type ActivityStatus = 'available' | 'partial' | 'unavailable'
export type ResetCreditStatus = 'available' | 'partial' | 'stale' | 'unavailable'
export type ResetCreditSnapshot = {
  status: ResetCreditStatus
  availableCount: number | null
  expiresAtUnixSeconds: number | null
}
export type QuotaWindow = {
  remainingPercent: number
  windowSeconds: number
  resetsAtUnixSeconds: number | null
}
export type DashboardSnapshot = {
  quotaStatus: QuotaStatus
  planLabel: string | null
  quotaWindows: QuotaWindow[]
  quotaFetchedAtUnixSeconds: number | null
  retryAfterSeconds: number | null
  activityStatus: ActivityStatus
  activityTotalTokens: number | null
  activityBuckets: Array<number | null>
  activityScannedAtUnixSeconds: number | null
  resetCredits: ResetCreditSnapshot
}

export function splitDayEdgesUtcSeconds(startEpochMs: number, endEpochMs: number) {
  const durationMs = endEpochMs - startEpochMs
  if (!Number.isSafeInteger(startEpochMs) || !Number.isSafeInteger(endEpochMs) || durationMs < 23 * 60 * 60 * 1000 || durationMs > 25 * 60 * 60 * 1000) {
    throw new Error('Invalid local-day range')
  }
  const edges = Array.from({ length: 13 }, (_, index) => Math.round((startEpochMs + durationMs * index / 12) / 1000))
  if (!edges.every((edge, index) => Number.isSafeInteger(edge) && (index === 0 || edge > edges[index - 1]!))) throw new Error('Invalid local-day edges')
  return edges
}

export function localDayEdgesUtcSeconds(now = new Date()) {
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime()
  const end = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1).getTime()
  return splitDayEdgesUtcSeconds(start, end)
}

export function activityXAxisTickLabels(dayEdgesUtcSeconds: readonly number[], language: Language) {
  if (dayEdgesUtcSeconds.length !== 13) throw new Error('Expected 13 local-day bucket edges')
  const locale = language === 'zh' ? 'zh-CN' : 'en-AU'
  return [0, 3, 6, 9].map(index => new Date(dayEdgesUtcSeconds[index]! * 1000).toLocaleTimeString(locale, {
    hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }))
}

export function formatCompactTokenCount(value: number, roundDown = false) {
  if (!Number.isFinite(value) || value <= 0) return '0'
  if (value < 1_000) return Math.round(value).toLocaleString('en-AU')
  let divisor = value >= 1_000_000 ? 1_000_000 : 1_000
  let tenths = roundDown ? Math.floor(value / divisor * 10) : Math.round(value / divisor * 10)
  if (!roundDown && divisor === 1_000 && tenths >= 10_000) {
    divisor = 1_000_000
    tenths = Math.round(value / divisor * 10)
  }
  return `${(tenths / 10).toFixed(1)}${divisor === 1_000_000 ? 'M' : 'K'}`
}

export function formatActivityAxisTick(value: number) {
  return formatCompactTokenCount(value)
}

export function activityChartScale(maxValue: number) {
  if (!Number.isFinite(maxValue) || maxValue <= 0) return 3_000
  const roughStep = maxValue / 3
  const magnitude = 10 ** Math.floor(Math.log10(roughStep))
  const normalized = roughStep / magnitude
  const niceStep = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 5 ? 5 : 10
  return Math.max(3_000, niceStep * magnitude * 3)
}

export const TOKEN_BARS = [180, 420, 640, 880, 1320, 1680, 2460, 1740, 1280, 920, 600, 360]
export const TOKEN_TOTAL = TOKEN_BARS.reduce((sum, value) => sum + value, 0)

export const copy = {
  en: {
    plus: 'Plus', pro: 'Pro', fiveHour: '5h', weekly: 'Weekly', fiveHourShort: '5h', weeklyShort: 'wk', fiveHourFull: '5-hour', weeklyFull: 'Weekly',
    resets: 'Resets in', hours: '1h 42m', days: '4d 7h', remaining: 'remaining',
    quotaRemaining: 'Quota remaining', planSelection: 'Sample plan', today: 'Today',
    tokens: 'Local activity', device: 'This device only', tokenUnit: 'tokens', tokensPerBucket: 'Tokens / 2h', liveTokensPerBucket: 'Tokens / local-time bucket',
    presence: 'Codex app', notConnected: 'Not connected', demo: 'Sample data',
    refreshedLocally: 'Sample refreshed locally', refresh: 'Refresh sample', refreshLive: 'Refresh', pinUnavailable: 'Pin unavailable in demo',
    settings: 'Settings', done: 'Done', theme: 'Theme', system: 'System', light: 'Light', dark: 'Dark', language: 'Language',
    interval: 'Refresh interval', manual: 'Manual only', interval60: '60 sec', interval120: '120 sec', interval300: '300 sec', follow: 'Follow Codex', followHelp: 'Show when Codex opens.',
    alerts: 'Low-quota alerts', alertHelp: 'Notify at 10% remaining.', unavailable: 'Unavailable', unavailableDemo: 'Unavailable in demo',
    savedLocally: 'Saved locally', about: 'About', resetOpportunities: '2 reset opportunities', resetExpiry: 'Expires 31 Oct 2026',
    openDetails: 'Open quota details', quotaDetails: 'Quota details',
    chartAccessible: '12 synthetic two-hour token buckets from 0 to 3000 tokens',
    quotaStatus: 'Quota', loading: 'Loading…', unknown: 'Unknown', updated: 'Updated', partial: 'Partial', stale: 'Stale',
    unavailableLive: 'Unavailable', signedOut: 'Sign in required', rateLimited: 'Rate limited', refreshFailed: 'Refresh failed',
    refreshing: 'Refreshing…', refreshComplete: 'Quota updated', refreshIncompletePartial: 'Quota partially updated', refreshIncompleteStale: 'Refresh incomplete · showing stale data', refreshIncompleteUnavailable: 'Refresh incomplete · data unavailable',
    noQuotaWindows: 'No quota windows available', creditResetNotRead: 'Credit reset details not read', creditResetAvailable: 'Reset credits available: {count}', creditResetExpires: ' · next expires in {time}', creditResetPartial: 'Reset credits: {count} · expiry unavailable', creditResetStale: 'Reset credit data is stale',
    activityUnavailable: 'Unavailable', activityPartialUnknown: 'Partial · total unknown',
    liveChartAccessible: '12 local-device token buckets; blank positions have no available bucket data',
    activityEmpty: 'Activity data unavailable', activityPartialEmpty: 'Partial · bucket data unavailable', activityCoveragePartial: 'Partial data', peak: 'Peak',
    resetUnavailable: 'Reset time unavailable',
  },
  zh: {
    plus: 'Plus', pro: 'Pro', fiveHour: '5小时', weekly: '每周', fiveHourShort: '5小时', weeklyShort: '周', fiveHourFull: '5小时', weeklyFull: '每周',
    resets: '重置时间', hours: '1小时42分', days: '4天7小时', remaining: '剩余',
    quotaRemaining: '剩余额度', planSelection: '示例套餐', today: '今天',
    tokens: '本地活动', device: '仅此设备', tokenUnit: 'tokens', tokensPerBucket: '每2小时 tokens', liveTokensPerBucket: '每个本地时间区间 tokens',
    presence: 'Codex 应用', notConnected: '未连接', demo: '样本数据',
    refreshedLocally: '样本已在本地刷新', refresh: '刷新样本', refreshLive: '刷新', pinUnavailable: '演示中无法固定',
    settings: '设置', done: '完成', theme: '主题', system: '系统', light: '浅色', dark: '深色', language: '语言',
    interval: '刷新间隔', manual: '仅手动', interval60: '60秒', interval120: '120秒', interval300: '300秒', follow: '跟随 Codex', followHelp: 'Codex 打开时显示。',
    alerts: '低额度提醒', alertHelp: '剩余 10% 时提醒。', unavailable: '不可用', unavailableDemo: '演示中不可用',
    savedLocally: '已保存在本地', about: '关于', resetOpportunities: '2 次重置机会', resetExpiry: '10月31日到期',
    openDetails: '打开额度详情', quotaDetails: '额度详情',
    chartAccessible: '12个合成的两小时 token 区间，范围为0至3000 tokens',
    quotaStatus: '额度', loading: '加载中…', unknown: '未知', updated: '更新于', partial: '部分数据', stale: '数据过期',
    unavailableLive: '不可用', signedOut: '需要登录', rateLimited: '请求受限', refreshFailed: '刷新失败',
    refreshing: '正在刷新…', refreshComplete: '额度已更新', refreshIncompletePartial: '额度部分更新', refreshIncompleteStale: '刷新不完整 · 当前显示过期数据', refreshIncompleteUnavailable: '刷新不完整 · 数据不可用',
    noQuotaWindows: '没有可用的额度区间', creditResetNotRead: '额度重置信息未读取', creditResetAvailable: '可用重置额度：{count}', creditResetExpires: ' · 最近一次将在 {time} 后过期', creditResetPartial: '重置额度：{count} · 过期时间不可用', creditResetStale: '重置额度数据已过期',
    activityUnavailable: '不可用', activityPartialUnknown: '部分数据 · 总量未知',
    liveChartAccessible: '12个仅限本设备的 token 区间；空白位置表示没有可用区间数据',
    activityEmpty: '本地活动数据不可用', activityPartialEmpty: '部分数据 · 区间数据不可用', activityCoveragePartial: '部分数据', peak: '峰值',
    resetUnavailable: '重置时间不可用',
  },
} as const
export type Copy = (typeof copy)[Language]

export function visibleQuota(plan: Plan) {
  return plan === 'plus'
    ? [{ key: 'fiveHour' as const, value: 74, color: 'mint', reset: 'hours' as const }, { key: 'weekly' as const, value: 38, color: 'violet', reset: 'days' as const }]
    : [{ key: 'weekly' as const, value: 38, color: 'violet', reset: 'days' as const }]
}
