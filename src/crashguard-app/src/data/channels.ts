export interface SlackConfig {
  webhookUrl: string
}

export interface EmailConfig {
  addresses: string[]
}

export interface WebhookConfig {
  url: string
}

export type ChannelType = 'slack' | 'email' | 'webhook'

export type ChannelConfig<T extends ChannelType = ChannelType> = T extends 'slack'
  ? SlackConfig
  : T extends 'email'
  ? EmailConfig
  : T extends 'webhook'
  ? WebhookConfig
  : never

export interface Channel<T extends ChannelType = ChannelType> {
  id: number
  name: string
  type: T
  config: ChannelConfig<T>
  createdAt: string
}

export function emptyConfig(type: ChannelType): ChannelConfig {
  switch (type) {
    case 'slack':
      return { webhookUrl: '' }
    case 'email':
      return { addresses: [''] }
    case 'webhook':
      return { url: '' }
  }
}
