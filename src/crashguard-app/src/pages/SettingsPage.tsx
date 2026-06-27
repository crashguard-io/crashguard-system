import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getSettings, updateSettings } from '../api/settings'

const inputClass =
  'mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500'

export default function SettingsPage() {
  const queryClient = useQueryClient()
  const [adminPortalUrl, setAdminPortalUrl] = useState('')
  const [smtpHost, setSmtpHost] = useState('')
  const [smtpPort, setSmtpPort] = useState('')
  const [smtpUsername, setSmtpUsername] = useState('')
  const [smtpPassword, setSmtpPassword] = useState('')
  const [smtpFromAddress, setSmtpFromAddress] = useState('')
  const [smtpFromName, setSmtpFromName] = useState('')
  const [smtpUseTls, setSmtpUseTls] = useState(true)
  const [resolvedRetentionEnabled, setResolvedRetentionEnabled] = useState(false)
  const [resolvedRetentionDays, setResolvedRetentionDays] = useState('30')
  const [triggeredRetentionEnabled, setTriggeredRetentionEnabled] = useState(false)
  const [triggeredRetentionDays, setTriggeredRetentionDays] = useState('30')

  const { data: settings } = useQuery({
    queryKey: ['settings'],
    queryFn: getSettings,
  })

  useEffect(() => {
    if (!settings) return
    setAdminPortalUrl(settings.adminPortalUrl ?? '')
    setSmtpHost(settings.smtpHost ?? '')
    setSmtpPort(settings.smtpPort?.toString() ?? '')
    setSmtpUsername(settings.smtpUsername ?? '')
    setSmtpPassword(settings.smtpPassword ?? '')
    setSmtpFromAddress(settings.smtpFromAddress ?? '')
    setSmtpFromName(settings.smtpFromName ?? '')
    setSmtpUseTls(settings.smtpUseTls)
    setResolvedRetentionEnabled(settings.resolvedRetentionDays !== null)
    if (settings.resolvedRetentionDays !== null) setResolvedRetentionDays(settings.resolvedRetentionDays.toString())
    setTriggeredRetentionEnabled(settings.triggeredRetentionDays !== null)
    if (settings.triggeredRetentionDays !== null) setTriggeredRetentionDays(settings.triggeredRetentionDays.toString())
  }, [settings])

  const saveMutation = useMutation({
    mutationFn: () => updateSettings({
      adminPortalUrl: adminPortalUrl.trim() || null,
      smtpHost: smtpHost.trim() || null,
      smtpPort: smtpPort.trim() ? Number(smtpPort) : null,
      smtpUsername: smtpUsername.trim() || null,
      smtpPassword: smtpPassword.trim() || null,
      smtpFromAddress: smtpFromAddress.trim() || null,
      smtpFromName: smtpFromName.trim() || null,
      smtpUseTls,
      resolvedRetentionDays: resolvedRetentionEnabled ? Number(resolvedRetentionDays) || null : null,
      triggeredRetentionDays: triggeredRetentionEnabled ? Number(triggeredRetentionDays) || null : null,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    saveMutation.mutate()
  }

  return (
    <div className="max-w-2xl mx-auto w-full">
      <h1 className="text-2xl font-semibold text-white">Settings</h1>
      <p className="text-gray-400 mt-1">Configure your CrashGuard.io instance.</p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-6">
        <div>
          <label htmlFor="admin-portal-url" className="block text-sm font-medium text-gray-300">
            Admin portal URL
          </label>
          <input
            id="admin-portal-url"
            type="text"
            value={adminPortalUrl}
            onChange={e => setAdminPortalUrl(e.target.value)}
            placeholder="http://my-host:8080"
            className="mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500"
          />
          <p className="mt-1.5 text-xs text-gray-500">
            The URL you use to reach this admin portal in your browser, including whatever port
            you mapped it to. Alerts (Slack, email, etc.) use this to link directly to the canary
            that triggered them.
          </p>
          <p className="mt-1.5 text-xs text-gray-500">
            This can't be auto-detected: the engine and this portal run inside the same Docker
            container, behind an internal reverse proxy, so the engine has no way to know which
            host port you exposed them on when you started the container. If you remap ports (for
            example <span className="font-mono">-p 9080:80</span> instead of the default{' '}
            <span className="font-mono">80:80</span>), update this value to match — otherwise alert
            links will point to the wrong address. Leave it blank to omit links from alerts.
          </p>
        </div>

        <div className="border-t border-gray-700 pt-6">
          <h2 className="text-lg font-medium text-white">Email (SMTP)</h2>
          <p className="mt-1 text-xs text-gray-500">
            Configured once for the whole instance. Email channels only need to name a recipient
            address — alerts are sent through this relay.
          </p>

          <div className="mt-4 grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="smtp-host" className="block text-sm font-medium text-gray-300">
                Host
              </label>
              <input
                id="smtp-host"
                type="text"
                value={smtpHost}
                onChange={e => setSmtpHost(e.target.value)}
                placeholder="smtp.example.com"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="smtp-port" className="block text-sm font-medium text-gray-300">
                Port
              </label>
              <input
                id="smtp-port"
                type="number"
                value={smtpPort}
                onChange={e => setSmtpPort(e.target.value)}
                placeholder="587"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="smtp-username" className="block text-sm font-medium text-gray-300">
                Username
              </label>
              <input
                id="smtp-username"
                type="text"
                value={smtpUsername}
                onChange={e => setSmtpUsername(e.target.value)}
                placeholder="(optional)"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="smtp-password" className="block text-sm font-medium text-gray-300">
                Password
              </label>
              <input
                id="smtp-password"
                type="password"
                value={smtpPassword}
                onChange={e => setSmtpPassword(e.target.value)}
                placeholder="(optional)"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="smtp-from-address" className="block text-sm font-medium text-gray-300">
                From address
              </label>
              <input
                id="smtp-from-address"
                type="email"
                value={smtpFromAddress}
                onChange={e => setSmtpFromAddress(e.target.value)}
                placeholder="alerts@example.com"
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="smtp-from-name" className="block text-sm font-medium text-gray-300">
                From name
              </label>
              <input
                id="smtp-from-name"
                type="text"
                value={smtpFromName}
                onChange={e => setSmtpFromName(e.target.value)}
                placeholder="CrashGuard"
                className={inputClass}
              />
            </div>
          </div>

          <label className="mt-4 flex items-center gap-2 text-sm text-gray-300">
            <input
              type="checkbox"
              checked={smtpUseTls}
              onChange={e => setSmtpUseTls(e.target.checked)}
              className="accent-green-500"
            />
            Use TLS
          </label>
        </div>

        <div className="border-t border-gray-700 pt-6">
          <h2 className="text-lg font-medium text-white">Canary Retention</h2>
          <p className="mt-1 text-xs text-gray-500">
            Automatically delete canaries that have reached a terminal status once they're older
            than the configured number of days. Leave unchecked to keep them indefinitely.
          </p>

          <div className="mt-4 flex flex-col gap-4">
            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 text-sm text-gray-300 shrink-0">
                <input
                  type="checkbox"
                  checked={resolvedRetentionEnabled}
                  onChange={e => setResolvedRetentionEnabled(e.target.checked)}
                  className="accent-green-500"
                />
                Delete Resolved canaries after
              </label>
              <input
                type="number"
                min="1"
                value={resolvedRetentionDays}
                disabled={!resolvedRetentionEnabled}
                onChange={e => setResolvedRetentionDays(e.target.value)}
                className="w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500 disabled:opacity-40"
              />
              <span className="text-sm text-gray-400">days</span>
            </div>

            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 text-sm text-gray-300 shrink-0">
                <input
                  type="checkbox"
                  checked={triggeredRetentionEnabled}
                  onChange={e => setTriggeredRetentionEnabled(e.target.checked)}
                  className="accent-green-500"
                />
                Delete Triggered canaries after
              </label>
              <input
                type="number"
                min="1"
                value={triggeredRetentionDays}
                disabled={!triggeredRetentionEnabled}
                onChange={e => setTriggeredRetentionDays(e.target.value)}
                className="w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500 disabled:opacity-40"
              />
              <span className="text-sm text-gray-400">days</span>
            </div>
          </div>
        </div>

        {saveMutation.isError && (
          <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
            Failed to save settings. {(saveMutation.error as Error).message}
          </div>
        )}

        {saveMutation.isSuccess && !saveMutation.isPending && (
          <div className="rounded border border-green-500/30 bg-green-500/10 px-4 py-3 text-sm text-green-400">
            Settings saved.
          </div>
        )}

        <div className="flex gap-3 pt-2">
          <button
            type="submit"
            disabled={saveMutation.isPending}
            className="px-4 py-2 rounded bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Save Changes
          </button>
        </div>
      </form>
    </div>
  )
}
