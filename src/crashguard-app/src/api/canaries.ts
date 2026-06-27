import { apiFetch } from './client'

export type CanaryStatus = 'Pending' | 'Resolved' | 'Triggered'

export interface CanaryDto {
  id: number
  canaryType: string
  referenceId: string
  status: CanaryStatus
  startedAt: string
  resolvedAt: string | null
  triggeredAt: string | null
  timeout: number
  expiresAt: string
  metadata: unknown
}

export interface PagedResultDto<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CanarySummaryDto {
  pendingCount: number
  resolvedCount: number
  triggeredCount: number
  atRisk: CanaryDto[]
  recent: CanaryDto[]
}

export interface CanaryCheckpointDto {
  id: number
  canaryId: number
  stage: string
  metadata: unknown
  recordedAt: string
}

export function listCanariesPaged(
  status: CanaryStatus | undefined,
  page: number,
  pageSize: number,
  canaryType?: string,
  since?: string,
  until?: string,
) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (status) params.set('status', status)
  if (canaryType) params.set('canaryType', canaryType)
  if (since) params.set('since', since)
  if (until) params.set('until', until)
  return apiFetch<PagedResultDto<CanaryDto>>(`/api/canaries?${params.toString()}`)
}

export function getCanarySummary() {
  return apiFetch<CanarySummaryDto>('/api/canaries/summary')
}

export function getCanary(canaryType: string, referenceId: string) {
  return apiFetch<CanaryDto>(`/api/canaries/${encodeURIComponent(canaryType)}/${encodeURIComponent(referenceId)}`)
}

export function getCanaryCheckpoints(canaryType: string, referenceId: string) {
  return apiFetch<CanaryCheckpointDto[]>(
    `/api/canaries/${encodeURIComponent(canaryType)}/${encodeURIComponent(referenceId)}/checkpoints`
  )
}
