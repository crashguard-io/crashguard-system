import type { ReactElement } from 'react'
import type { ChannelConfig, ChannelType, EmailConfig, SlackConfig, WebhookConfig } from './channels'

const inputClass =
  'mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500'

interface ConfigFieldsProps<T> {
  config: T
  onChange: (config: T) => void
}

export interface Connector<T extends ChannelType = ChannelType> {
  type: T
  label: string
  description: string
  ConfigFields: (props: ConfigFieldsProps<ChannelConfig<T>>) => ReactElement
}

const slackConnector: Connector<'slack'> = {
  type: 'slack',
  label: 'Slack',
  description: 'Post alerts to a Slack channel via an incoming webhook.',
  ConfigFields: ({ config, onChange }: ConfigFieldsProps<SlackConfig>) => (
    <div>
      <label htmlFor="webhookUrl" className="block text-sm font-medium text-gray-300">
        Webhook URL
      </label>
      <input
        id="webhookUrl"
        type="url"
        required
        value={config.webhookUrl}
        onChange={e => onChange({ ...config, webhookUrl: e.target.value })}
        placeholder="https://hooks.slack.com/services/..."
        className={inputClass}
      />
    </div>
  ),
}

const emailConnector: Connector<'email'> = {
  type: 'email',
  label: 'Email',
  description: 'Send a plain email to one or more addresses.',
  ConfigFields: ({ config, onChange }: ConfigFieldsProps<EmailConfig>) => {
    const addresses = config.addresses.length > 0 ? config.addresses : ['']

    function setAddress(index: number, value: string) {
      const next = [...addresses]
      next[index] = value
      onChange({ ...config, addresses: next })
    }

    function addAddress() {
      onChange({ ...config, addresses: [...addresses, ''] })
    }

    function removeAddress(index: number) {
      onChange({ ...config, addresses: addresses.filter((_, i) => i !== index) })
    }

    return (
      <div>
        <label className="block text-sm font-medium text-gray-300">Addresses</label>
        <div className="mt-1.5 flex flex-col gap-2">
          {addresses.map((address, index) => (
            <div key={index} className="flex gap-2">
              <input
                type="email"
                required
                value={address}
                onChange={e => setAddress(index, e.target.value)}
                placeholder="ops@example.com"
                className={inputClass}
              />
              <button
                type="button"
                onClick={() => removeAddress(index)}
                disabled={addresses.length === 1}
                className="px-3 rounded bg-gray-800 border border-gray-700 text-gray-400 hover:text-white hover:border-gray-600 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                &times;
              </button>
            </div>
          ))}
        </div>
        <button
          type="button"
          onClick={addAddress}
          className="mt-2 text-sm text-green-500 hover:text-green-400"
        >
          + Add address
        </button>
      </div>
    )
  },
}

const webhookConnector: Connector<'webhook'> = {
  type: 'webhook',
  label: 'Webhook',
  description: 'POST a JSON payload with the canary\'s info (including its metadata) to a URL.',
  ConfigFields: ({ config, onChange }: ConfigFieldsProps<WebhookConfig>) => (
    <div>
      <label htmlFor="url" className="block text-sm font-medium text-gray-300">
        URL
      </label>
      <input
        id="url"
        type="url"
        required
        value={config.url}
        onChange={e => onChange({ ...config, url: e.target.value })}
        placeholder="https://example.com/hooks/crashguard"
        className={inputClass}
      />
    </div>
  ),
}

export const connectorRegistry: Record<ChannelType, Connector> = {
  slack: slackConnector as Connector,
  email: emailConnector as Connector,
  webhook: webhookConnector as Connector,
}

export const connectorList = Object.values(connectorRegistry)
