import { apiFetch } from './client'
import type { CanarySeverity } from '../data/canaryTypes'
import type { Rule, RuleOperator, RuleSeverity } from '../components/RuleEditor'
import type { CanaryDto, CanaryStatus } from './canaries'

export interface CanaryTypeRuleDto {
  id: number
  field: string
  operator: RuleOperator
  value: string | null
  severity: RuleSeverity
  channel: string
}

export interface CanaryTypeDto {
  id: number
  name: string
  timeout: number
  extendLimit: number
  dedupInterval: number
  renotifyInterval: number
  severity: CanarySeverity
  metadataSchema: string | null
  verifierUrl: string | null
  createdAt: string
  rules: CanaryTypeRuleDto[]
  defaultChannelIds: number[]
}

export interface CanaryTypeWithRules {
  name: string
  timeout: number
  extendLimit: number
  dedupInterval: number
  renotifyInterval: number
  severity: CanarySeverity
  metadataSchema: string | null
  verifierUrl: string | null
  rules: Rule[]
  defaultChannelIds: number[]
}

function toRuleRequest(rule: Rule) {
  return {
    field: rule.field,
    operator: rule.operator,
    value: rule.operator === 'exists' ? null : rule.value,
    severity: rule.severity,
    channel: rule.channel,
  }
}

export function listCanaryTypes() {
  return apiFetch<CanaryTypeDto[]>('/api/canary-types')
}

export function getCanaryType(id: number) {
  return apiFetch<CanaryTypeDto>(`/api/canary-types/${id}`)
}

export function createCanaryType(input: CanaryTypeWithRules) {
  return apiFetch<CanaryTypeDto>('/api/canary-types', {
    method: 'POST',
    body: JSON.stringify({ ...input, rules: input.rules.map(toRuleRequest) }),
  })
}

export function updateCanaryType(id: number, input: CanaryTypeWithRules) {
  return apiFetch<CanaryTypeDto>(`/api/canary-types/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ ...input, rules: input.rules.map(toRuleRequest) }),
  })
}

export function deleteCanaryType(id: number) {
  return apiFetch<void>(`/api/canary-types/${id}`, { method: 'DELETE' })
}

export function listCanaryTypeCanaries(
  name: string,
  status?: CanaryStatus,
  limit = 50,
  since?: string,
  until?: string,
) {
  const params = new URLSearchParams({ limit: String(limit) })
  if (status) params.set('status', status)
  if (since) params.set('since', since)
  if (until) params.set('until', until)
  return apiFetch<CanaryDto[]>(`/api/canary-types/${encodeURIComponent(name)}/canaries?${params}`)
}
