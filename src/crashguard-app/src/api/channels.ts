import { apiFetch } from './client'
import type { Channel, ChannelConfig, ChannelType } from '../data/channels'

export function listChannels() {
  return apiFetch<Channel[]>('/api/channels')
}

export function getChannel(id: number) {
  return apiFetch<Channel>(`/api/channels/${id}`)
}

export interface ChannelInput {
  name: string
  type: ChannelType
  config: ChannelConfig
}

export function createChannel(input: ChannelInput) {
  return apiFetch<Channel>('/api/channels', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateChannel(id: number, input: ChannelInput) {
  return apiFetch<Channel>(`/api/channels/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })
}

export function deleteChannel(id: number) {
  return apiFetch<void>(`/api/channels/${id}`, { method: 'DELETE' })
}
