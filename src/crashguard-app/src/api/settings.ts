import { apiFetch } from './client'

export interface Settings {
  adminPortalUrl: string | null
  smtpHost: string | null
  smtpPort: number | null
  smtpUsername: string | null
  smtpPassword: string | null
  smtpFromAddress: string | null
  smtpFromName: string | null
  smtpUseTls: boolean
  resolvedRetentionDays: number | null
  triggeredRetentionDays: number | null
}

export function getSettings() {
  return apiFetch<Settings>('/api/settings')
}

export function updateSettings(input: Settings) {
  return apiFetch<Settings>('/api/settings', {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}
