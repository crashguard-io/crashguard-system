import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { emptyConfig, type ChannelConfig, type ChannelType } from '../data/channels'
import { connectorRegistry, connectorList } from '../data/connectors'
import { createChannel, getChannel, updateChannel } from '../api/channels'

export default function ChannelEditorPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { id } = useParams<{ id: string }>()
  const isEditing = id !== undefined
  const channelId = isEditing ? Number(id) : undefined

  const [name, setName] = useState('')
  const [type, setType] = useState<ChannelType>('slack')
  const [config, setConfig] = useState<ChannelConfig>(emptyConfig('slack'))

  const { data: existing } = useQuery({
    queryKey: ['channels', channelId],
    queryFn: () => getChannel(channelId!),
    enabled: channelId !== undefined,
  })

  useEffect(() => {
    if (!existing) return

    setName(existing.name)
    setType(existing.type)
    setConfig(existing.config)
  }, [existing])

  function handleTypeChange(nextType: ChannelType) {
    setType(nextType)
    setConfig(emptyConfig(nextType))
  }

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = { name, type, config }
      return channelId !== undefined
        ? updateChannel(channelId, payload)
        : createChannel(payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['channels'] })
      navigate('/channels')
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    saveMutation.mutate()
  }

  const connector = connectorRegistry[type]

  return (
    <div className="max-w-2xl mx-auto w-full">
      <h1 className="text-2xl font-semibold text-white">
        {isEditing ? 'Edit Channel' : 'Add Channel'}
      </h1>
      <p className="text-gray-400 mt-1">
        Define a named destination for alerts, backed by a specific connector.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-6">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-300">
            Name
          </label>
          <input
            id="name"
            type="text"
            required
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="ops-critical"
            className="mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-300">Connector</label>
          <div className="mt-1.5 grid grid-cols-3 gap-2">
            {connectorList.map(c => (
              <button
                key={c.type}
                type="button"
                onClick={() => handleTypeChange(c.type)}
                className={`rounded border px-3 py-2 text-sm font-medium text-left transition-colors ${
                  type === c.type
                    ? 'border-green-500 bg-green-500/10 text-white'
                    : 'border-gray-700 text-gray-400 hover:text-white hover:border-gray-600'
                }`}
              >
                {c.label}
              </button>
            ))}
          </div>
          <p className="mt-1.5 text-xs text-gray-500">{connector.description}</p>
        </div>

        <connector.ConfigFields config={config} onChange={setConfig} />

        {saveMutation.isError && (
          <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
            Failed to save channel. {(saveMutation.error as Error).message}
          </div>
        )}

        <div className="flex gap-3 pt-2">
          <button
            type="submit"
            disabled={saveMutation.isPending}
            className="px-4 py-2 rounded bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {isEditing ? 'Save Changes' : 'Create Channel'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/channels')}
            className="px-4 py-2 rounded bg-gray-700 hover:bg-gray-600 text-white text-sm font-medium transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
