import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createColumnHelper,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  flexRender,
  type SortingState,
} from '@tanstack/react-table'
import type { CanarySeverity } from '../data/canaryTypes'
import type { Channel } from '../data/channels'
import RuleEditor, { type Rule } from '../components/RuleEditor'
import { createCanaryType, getCanaryType, updateCanaryType } from '../api/canaryTypes'
import { listChannels } from '../api/channels'

const columnHelper = createColumnHelper<Channel>()

function parseTimeout(seconds: number) {
  return {
    hours: Math.floor(seconds / 3600),
    minutes: Math.floor((seconds % 3600) / 60),
    seconds: seconds % 60,
  }
}

export default function CanaryEditorPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { id } = useParams<{ id: string }>()
  const isEditing = id !== undefined
  const canaryTypeId = isEditing ? Number(id) : undefined

  const [name, setName] = useState('')
  const [hours, setHours] = useState(0)
  const [minutes, setMinutes] = useState(5)
  const [seconds, setSeconds] = useState(0)
  const [dedupHours, setDedupHours] = useState(0)
  const [dedupMinutes, setDedupMinutes] = useState(0)
  const [dedupSeconds, setDedupSeconds] = useState(0)
  const [renotifyHours, setRenotifyHours] = useState(0)
  const [renotifyMinutes, setRenotifyMinutes] = useState(0)
  const [renotifySeconds, setRenotifySeconds] = useState(0)
  const [extendLimit, setExtendLimit] = useState(0)
  const [severity, setSeverity] = useState<CanarySeverity>('warning')
  const [metadataSchema, setMetadataSchema] = useState('')
  const [useVerifier, setUseVerifier] = useState(false)
  const [verifierUrl, setVerifierUrl] = useState('')
  const [rules, setRules] = useState<Rule[]>([])
  const [defaultChannelIds, setDefaultChannelIds] = useState<number[]>([])
  const [sorting, setSorting] = useState<SortingState>([
    { id: 'type', desc: false },
    { id: 'name', desc: false },
  ])

  const { data: existing } = useQuery({
    queryKey: ['canary-types', canaryTypeId],
    queryFn: () => getCanaryType(canaryTypeId!),
    enabled: canaryTypeId !== undefined,
  })

  const { data: channels } = useQuery({
    queryKey: ['channels'],
    queryFn: listChannels,
  })

  function toggleDefaultChannel(channelId: number) {
    setDefaultChannelIds(ids =>
      ids.includes(channelId) ? ids.filter(id => id !== channelId) : [...ids, channelId]
    )
  }

  const columns = useMemo(
    () => [
      columnHelper.display({
        id: 'select',
        header: () => null,
        cell: ({ row }) => (
          <input
            type="checkbox"
            checked={defaultChannelIds.includes(row.original.id)}
            onChange={() => toggleDefaultChannel(row.original.id)}
            className="accent-green-500"
          />
        ),
      }),
      columnHelper.accessor('type', {
        header: 'Connector Type',
        cell: info => (
          <span className="text-gray-400 block truncate" title={info.getValue()}>
            {info.getValue()}
          </span>
        ),
      }),
      columnHelper.accessor('name', {
        header: 'Channel Name',
        cell: info => <span className="font-mono">{info.getValue()}</span>,
      }),
    ],
    [defaultChannelIds]
  )

  const table = useReactTable({
    data: channels ?? [],
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  useEffect(() => {
    if (!existing) return

    setName(existing.name)
    const t = parseTimeout(existing.timeout)
    setHours(t.hours)
    setMinutes(t.minutes)
    setSeconds(t.seconds)
    const d = parseTimeout(existing.dedupInterval)
    setDedupHours(d.hours)
    setDedupMinutes(d.minutes)
    setDedupSeconds(d.seconds)
    const r = parseTimeout(existing.renotifyInterval)
    setRenotifyHours(r.hours)
    setRenotifyMinutes(r.minutes)
    setRenotifySeconds(r.seconds)
    setExtendLimit(existing.extendLimit)
    setSeverity(existing.severity)
    setMetadataSchema(existing.metadataSchema ?? '')
    setUseVerifier(existing.verifierUrl !== null)
    setVerifierUrl(existing.verifierUrl ?? '')
    setRules(existing.rules.map(r => ({
      id: crypto.randomUUID(),
      field: r.field,
      operator: r.operator,
      value: r.value ?? '',
      severity: r.severity,
      channel: r.channel,
    })))
    setDefaultChannelIds(existing.defaultChannelIds)
  }, [existing])

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = {
        name,
        timeout: hours * 3600 + minutes * 60 + seconds,
        dedupInterval: dedupHours * 3600 + dedupMinutes * 60 + dedupSeconds,
        renotifyInterval: renotifyHours * 3600 + renotifyMinutes * 60 + renotifySeconds,
        extendLimit,
        severity,
        metadataSchema: metadataSchema.trim() === '' ? null : metadataSchema,
        verifierUrl: useVerifier ? verifierUrl : null,
        rules,
        defaultChannelIds,
      }
      return canaryTypeId !== undefined
        ? updateCanaryType(canaryTypeId, payload)
        : createCanaryType(payload)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['canary-types'] })
      navigate('/canary-types')
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    saveMutation.mutate()
  }

  return (
    <div className="max-w-2xl mx-auto w-full">
      <h1 className="text-2xl font-semibold text-white">
        {isEditing ? 'Edit Canary' : 'Add Canary'}
      </h1>
      <p className="text-gray-400 mt-1">
        Configure a canary type, including its timeout and expected metadata shape.
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
            placeholder="order-fulfillment"
            className="mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500"
          />
        </div>

        <div className="flex flex-wrap gap-x-8 gap-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-300">Timeout</label>
            <div className="mt-1.5 flex items-center gap-2">
              <input
                type="number"
                min="0"
                value={hours}
                onChange={e => setHours(Number(e.target.value))}
                className="w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">h</span>
              <input
                type="number"
                min="0"
                max="59"
                value={minutes}
                onChange={e => setMinutes(Number(e.target.value))}
                className="w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">m</span>
              <input
                type="number"
                min="0"
                max="59"
                value={seconds}
                onChange={e => setSeconds(Number(e.target.value))}
                className="w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">s</span>
            </div>
            <p className="mt-1.5 text-xs text-gray-500">
              How long a canary of this type can stay pending before it's considered expired.
            </p>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-x-8 gap-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-300">Dedup Interval</label>
            <div className="mt-1.5 flex items-center gap-2">
              <input
                type="number"
                min="0"
                value={dedupHours}
                onChange={e => setDedupHours(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">h</span>
              <input
                type="number"
                min="0"
                max="59"
                value={dedupMinutes}
                onChange={e => setDedupMinutes(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">m</span>
              <input
                type="number"
                min="0"
                max="59"
                value={dedupSeconds}
                onChange={e => setDedupSeconds(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">s</span>
            </div>
            <p className="mt-1.5 text-xs text-gray-500">
              Quiet period with no new triggers of this type before a deduplicated alert batch closes.
              Zero disables deduplication: every trigger sends its own alert.
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300">Renotify Interval</label>
            <div className="mt-1.5 flex items-center gap-2">
              <input
                type="number"
                min="0"
                value={renotifyHours}
                onChange={e => setRenotifyHours(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">h</span>
              <input
                type="number"
                min="0"
                max="59"
                value={renotifyMinutes}
                onChange={e => setRenotifyMinutes(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">m</span>
              <input
                type="number"
                min="0"
                max="59"
                value={renotifySeconds}
                onChange={e => setRenotifySeconds(Number(e.target.value))}
                className="w-16 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
              />
              <span className="text-gray-500">s</span>
            </div>
            <p className="mt-1.5 text-xs text-gray-500">
              While a deduplicated batch stays open past this interval, send a "still firing" summary.
              Zero disables renotification: the batch stays silent until it closes.
            </p>
          </div>
        </div>

        <div>
          <label htmlFor="severity" className="block text-sm font-medium text-gray-300">
            Default Severity
          </label>
          <select
            id="severity"
            value={severity}
            onChange={e => setSeverity(e.target.value as CanarySeverity)}
            className="mt-1.5 w-48 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white focus:outline-none focus:border-green-500"
          >
            <option value="info">Info</option>
            <option value="warning">Warning</option>
            <option value="critical">Critical</option>
          </select>
          <p className="mt-1.5 text-xs text-gray-500">
            Default severity used when this canary expires and no rule matches.
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-300">
            Default Channels <span className="text-gray-500">(optional)</span>
          </label>
          <p className="mt-1.5 text-xs text-gray-500">
            Channels that get notified when a canary of this type expires without resolving, unless a rule overrides it.
          </p>
          {(channels?.length ?? 0) > 0 ? (
            <div className="mt-3 border border-gray-700 rounded overflow-hidden">
              <table className="w-full text-sm text-left border-collapse table-fixed">
                <colgroup>
                  <col className="w-10" />
                  <col style={{ width: '10rem' }} />
                  <col />
                </colgroup>
                <thead>
                  {table.getHeaderGroups().map(headerGroup => (
                    <tr key={headerGroup.id}>
                      {headerGroup.headers.map(header => (
                        <th
                          key={header.id}
                          onClick={header.column.getToggleSortingHandler()}
                          className={`h-9 py-2 px-2 font-medium text-gray-300 bg-gray-800 border border-gray-700 whitespace-nowrap ${
                            header.column.getCanSort() ? 'cursor-pointer select-none' : ''
                          }`}
                        >
                          {flexRender(header.column.columnDef.header, header.getContext())}
                          {{ asc: ' ▲', desc: ' ▼' }[header.column.getIsSorted() as string] ?? ''}
                        </th>
                      ))}
                    </tr>
                  ))}
                </thead>
              </table>
              <div className="overflow-y-auto" style={{ maxHeight: `${5 * 36}px` }}>
                <table className="w-full text-sm text-left border-collapse table-fixed">
                  <colgroup>
                    <col className="w-10" />
                    <col style={{ width: '10rem' }} />
                    <col />
                  </colgroup>
                  <tbody>
                    {table.getRowModel().rows.map(row => (
                      <tr key={row.id} className="h-9">
                        {row.getVisibleCells().map(cell => (
                          <td key={cell.id} className="py-2 px-2 border border-gray-700">
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </td>
                        ))}
                      </tr>
                    ))}
                    {Array.from({ length: Math.max(0, 5 - table.getRowModel().rows.length) }).map((_, i) => (
                      <tr key={`empty-${i}`} className="h-9">
                        {table.getVisibleLeafColumns().map(column => (
                          <td key={column.id} className="py-2 px-2 border border-gray-700">
                            &nbsp;
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <p className="mt-2 text-xs text-gray-500">No channels configured yet.</p>
          )}
        </div>

        <div>
          <label htmlFor="metadataSchema" className="block text-sm font-medium text-gray-300">
            Metadata Schema <span className="text-gray-500">(optional)</span>
          </label>
          <textarea
            id="metadataSchema"
            rows={6}
            value={metadataSchema}
            onChange={e => setMetadataSchema(e.target.value)}
            placeholder={'{\n  "type": "object",\n  "properties": {\n    "orderId": { "type": "number" }\n  }\n}'}
            className="mt-1.5 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500"
          />
          <p className="mt-1.5 text-xs text-gray-500">
            A JSON Schema describing the metadata shape clients should send when creating this canary.
          </p>
        </div>

        <div>
          <label className="flex items-center gap-2 text-sm font-medium text-gray-300">
            <input
              type="checkbox"
              checked={useVerifier}
              onChange={e => setUseVerifier(e.target.checked)}
              className="accent-green-500"
            />
            Use a verifier
          </label>
          {useVerifier && (
            <>
              <div className="mt-2 flex items-start gap-3">
                <div className="flex-1">
                  <label htmlFor="verifierUrl" className="block text-xs font-medium text-gray-300 whitespace-nowrap">
                    Verifier Url
                  </label>
                  <input
                    id="verifierUrl"
                    type="url"
                    required
                    value={verifierUrl}
                    onChange={e => setVerifierUrl(e.target.value)}
                    placeholder="https://api.example.com/verify/order"
                    className="mt-1 w-full rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500"
                  />
                </div>
                <div>
                  <label htmlFor="extendLimit" className="block text-xs font-medium text-gray-300 whitespace-nowrap">
                    Extend Limit
                  </label>
                  <input
                    id="extendLimit"
                    type="number"
                    min="0"
                    value={extendLimit}
                    onChange={e => setExtendLimit(Number(e.target.value))}
                    className="mt-1 w-20 rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono text-center focus:outline-none focus:border-green-500"
                  />
                </div>
              </div>
              <p className="mt-1.5 text-xs text-gray-500">
                Before resolving a canary of this type, CrashGuard will call this URL to verify the
                workflow-specific result before marking it healthy. Max number of times a canary of this
                type can be extended.
              </p>
            </>
          )}
        </div>

        <RuleEditor rules={rules} onChange={setRules} channels={channels ?? []} />

        {saveMutation.isError && (
          <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
            Failed to save canary type. {(saveMutation.error as Error).message}
          </div>
        )}

        <div className="flex gap-3 pt-2">
          <button
            type="submit"
            disabled={saveMutation.isPending}
            className="px-4 py-2 rounded bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {isEditing ? 'Save Changes' : 'Create Canary'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/canary-types')}
            className="px-4 py-2 rounded bg-gray-700 hover:bg-gray-600 text-white text-sm font-medium transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}
