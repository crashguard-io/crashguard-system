import { useEffect, useMemo, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { listCanariesPaged, type CanaryDto, type CanaryStatus } from '../api/canaries'
import { listCanaryTypes, type CanaryTypeDto } from '../api/canaryTypes'

function isoToDatetimeLocal(iso: string | undefined): string {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function datetimeLocalToIso(value: string): string | undefined {
  if (!value) return undefined
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return undefined
  return date.toISOString()
}

const ROW_HEIGHT_PX = 41
const HEADER_HEIGHT_PX = 41
const MIN_PAGE_SIZE = 1

function useRowsThatFit(containerRef: React.RefObject<HTMLDivElement | null>) {
  const [rowsThatFit, setRowsThatFit] = useState(MIN_PAGE_SIZE)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return

    function measure() {
      if (!el) return
      const available = el.clientHeight - HEADER_HEIGHT_PX
      const rows = Math.max(MIN_PAGE_SIZE, Math.floor(available / ROW_HEIGHT_PX))
      setRowsThatFit(rows)
    }

    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(el)
    return () => observer.disconnect()
  }, [containerRef])

  return rowsThatFit
}

const statusStyles: Record<CanaryStatus, string> = {
  Pending: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
  Resolved: 'bg-green-500/20 text-green-400 border-green-500/30',
  Triggered: 'bg-red-500/20 text-red-400 border-red-500/30',
}

function StatusBadge({ status }: { status: CanaryStatus }) {
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${statusStyles[status]}`}>
      {status}
    </span>
  )
}

function CanaryTable({
  canaries,
  emptyText,
  onRowClick,
  canaryTypesByName,
  selectedId,
}: {
  canaries: CanaryDto[]
  emptyText: string
  onRowClick?: (canary: CanaryDto) => void
  canaryTypesByName: Map<string, CanaryTypeDto>
  selectedId?: number
}) {
  if (canaries.length === 0) {
    return <p className="p-4 text-sm text-gray-500">{emptyText}</p>
  }

  return (
    <table className="w-full text-sm text-gray-300 border-collapse">
      <thead className="sticky top-0 bg-gray-800 text-gray-400 text-xs uppercase tracking-wider">
        <tr>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Canary Type</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Reference</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Started At</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Timeout</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Due At</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Has Verifier</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Extend Limit</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Status</th>
        </tr>
      </thead>
      <tbody>
        {canaries.map(canary => {
          const canaryType = canaryTypesByName.get(canary.canaryType)
          const hasVerifier = !!canaryType?.verifierUrl
          return (
            <tr
              key={canary.id}
              onClick={() => onRowClick?.(canary)}
              className={`border-b border-gray-700/50 transition-colors ${onRowClick ? 'cursor-pointer' : ''} ${
                canary.id === selectedId ? 'bg-blue-500/15 hover:bg-blue-500/20' : 'hover:bg-gray-800/60'
              }`}
            >
              <td className="px-4 py-3 font-mono text-sm">{canary.canaryType}</td>
              <td className="px-4 py-3 font-mono text-sm text-gray-400">{canary.referenceId}</td>
              <td className="px-4 py-3 text-sm">{new Date(canary.startedAt).toLocaleString()}</td>
              <td className="px-4 py-3 text-sm">{canary.timeout}s</td>
              <td className="px-4 py-3 text-sm">{new Date(canary.expiresAt).toLocaleString()}</td>
              <td className="px-4 py-3 text-sm">{hasVerifier ? 'True' : 'False'}</td>
              <td className="px-4 py-3 text-sm">{hasVerifier ? canaryType?.extendLimit : 'N/A'}</td>
              <td className="px-4 py-3"><StatusBadge status={canary.status} /></td>
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}

function getPageItems(page: number, totalPages: number): (number | '...')[] {
  const siblingCount = 1
  const items: (number | '...')[] = []

  const start = Math.max(2, page - siblingCount)
  const end = Math.min(totalPages - 1, page + siblingCount)

  items.push(1)
  if (start > 2) items.push('...')
  for (let p = start; p <= end; p++) items.push(p)
  if (end < totalPages - 1) items.push('...')
  if (totalPages > 1) items.push(totalPages)

  return items
}

function Pagination({
  page,
  totalCount,
  pageSize,
  onPageChange,
}: {
  page: number
  totalCount: number
  pageSize: number
  onPageChange: (page: number) => void
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null

  return (
    <div className="flex items-center justify-end gap-1 px-2 py-2">
      <button
        className="px-2 py-1 text-xs rounded text-gray-400 hover:bg-gray-800 disabled:opacity-40 disabled:hover:bg-transparent"
        onClick={() => onPageChange(page - 1)}
        disabled={page <= 1}
      >
        Prev
      </button>
      {getPageItems(page, totalPages).map((item, i) =>
        item === '...' ? (
          <span key={`ellipsis-${i}`} className="px-1.5 text-xs text-gray-500">
            …
          </span>
        ) : (
          <button
            key={item}
            className={`px-2.5 py-1 text-xs rounded ${
              item === page ? 'bg-blue-500/20 text-blue-400 border border-blue-500/30' : 'text-gray-400 hover:bg-gray-800'
            }`}
            onClick={() => onPageChange(item)}
          >
            {item}
          </button>
        )
      )}
      <button
        className="px-2 py-1 text-xs rounded text-gray-400 hover:bg-gray-800 disabled:opacity-40 disabled:hover:bg-transparent"
        onClick={() => onPageChange(page + 1)}
        disabled={page >= totalPages}
      >
        Next
      </button>
    </div>
  )
}

const statusFilters: ('All' | CanaryStatus)[] = ['All', 'Pending', 'Resolved', 'Triggered']

function parseStatusParam(value: string | null): 'All' | CanaryStatus {
  return value && (statusFilters as string[]).includes(value) ? (value as 'All' | CanaryStatus) : 'All'
}

function parsePageParam(value: string | null): number {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : 1
}

export default function CanariesPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const [status, setStatus] = useState<'All' | CanaryStatus>(parseStatusParam(searchParams.get('status')))
  const [canaryType, setCanaryType] = useState<string>(searchParams.get('canaryType') ?? 'All')
  // Keep the full-precision ISO value (as received from the URL, e.g. an alert link)
  // until the user actually edits a picker — the datetime-local inputs only round to
  // the minute for display/editing, they must not clobber the value used for querying.
  const [sinceIso, setSinceIso] = useState<string | undefined>(searchParams.get('since') ?? undefined)
  const [untilIso, setUntilIso] = useState<string | undefined>(searchParams.get('until') ?? undefined)
  const [page, setPage] = useState(parsePageParam(searchParams.get('page')))
  const [selectedId, setSelectedId] = useState<number | undefined>(
    searchParams.get('selected') ? Number(searchParams.get('selected')) : undefined,
  )
  const location = useLocation()
  const containerRef = useRef<HTMLDivElement>(null)
  const pageSize = useRowsThatFit(containerRef)

  const since = isoToDatetimeLocal(sinceIso)
  const until = isoToDatetimeLocal(untilIso)

  const { data: canaryTypes } = useQuery({
    queryKey: ['canary-types'],
    queryFn: listCanaryTypes,
  })

  const canaryTypesByName = useMemo(
    () => new Map(canaryTypes?.map(type => [type.name, type]) ?? []),
    [canaryTypes],
  )

  const { data, isLoading, isError } = useQuery({
    queryKey: ['canaries', 'all', status, canaryType, sinceIso, untilIso, page, pageSize],
    queryFn: () =>
      listCanariesPaged(
        status === 'All' ? undefined : status,
        page,
        pageSize,
        canaryType === 'All' ? undefined : canaryType,
        sinceIso,
        untilIso,
      ),
    refetchInterval: 15_000,
  })

  useEffect(() => {
    if (!data) return
    const totalPages = Math.max(1, Math.ceil(data.totalCount / pageSize))
    if (page > totalPages) setPage(totalPages)
  }, [data, pageSize, page])

  // Keep the URL in sync so filters (and the current page) are bookmarkable/shareable,
  // so the alert-link route from the engine works, and so navigating to a canary's
  // detail page and back restores the exact view the user left.
  useEffect(() => {
    const next = new URLSearchParams()
    if (status !== 'All') next.set('status', status)
    if (canaryType !== 'All') next.set('canaryType', canaryType)
    if (sinceIso) next.set('since', sinceIso)
    if (untilIso) next.set('until', untilIso)
    if (page !== 1) next.set('page', String(page))
    if (selectedId !== undefined) next.set('selected', String(selectedId))
    setSearchParams(next, { replace: true })
  }, [status, canaryType, sinceIso, untilIso, page, selectedId, setSearchParams])

  function changeStatus(next: 'All' | CanaryStatus) {
    setStatus(next)
    setPage(1)
  }

  function changeCanaryType(next: string) {
    setCanaryType(next)
    setPage(1)
  }

  function changeSince(next: string) {
    setSinceIso(datetimeLocalToIso(next))
    setPage(1)
  }

  function changeUntil(next: string) {
    setUntilIso(datetimeLocalToIso(next))
    setPage(1)
  }

  function clearDateRange() {
    setSinceIso(undefined)
    setUntilIso(undefined)
    setPage(1)
  }

  function goToDetail(canary: CanaryDto) {
    setSelectedId(canary.id)
    const params = new URLSearchParams(searchParams)
    params.set('selected', String(canary.id))
    navigate(`/canaries/${encodeURIComponent(canary.canaryType)}/${encodeURIComponent(canary.referenceId)}`, {
      state: { from: `${location.pathname}?${params.toString()}` },
    })
  }

  return (
    <div className="flex flex-col h-full gap-4 min-h-0">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-white">Canaries</h1>
          <p className="text-gray-400 mt-1">All canaries, most recent first.</p>
        </div>
        <div className="flex items-center gap-3">
          <select
            value={canaryType}
            onChange={e => changeCanaryType(e.target.value)}
            className="bg-gray-800 text-sm text-gray-300 border border-gray-700 rounded px-3 py-1.5 focus:outline-none focus:border-gray-500"
          >
            <option value="All">All Canary Types</option>
            {canaryTypes?.map(type => (
              <option key={type.id} value={type.name}>
                {type.name}
              </option>
            ))}
          </select>
          <div className="flex items-center gap-1.5">
            <input
              type="datetime-local"
              value={since}
              onChange={e => changeSince(e.target.value)}
              className="bg-gray-800 text-sm text-gray-300 border border-gray-700 rounded px-2 py-1.5 focus:outline-none focus:border-gray-500"
            />
            <span className="text-gray-500 text-sm">&ndash;</span>
            <input
              type="datetime-local"
              value={until}
              onChange={e => changeUntil(e.target.value)}
              className="bg-gray-800 text-sm text-gray-300 border border-gray-700 rounded px-2 py-1.5 focus:outline-none focus:border-gray-500"
            />
            {(since || until) && (
              <button
                onClick={clearDateRange}
                className="text-xs text-gray-400 hover:text-white px-1.5"
                title="Clear date range"
              >
                ✕
              </button>
            )}
          </div>
          <div className="flex gap-1">
            {statusFilters.map(filter => (
              <button
                key={filter}
                onClick={() => changeStatus(filter)}
                className={`px-3 py-1.5 rounded text-sm transition-colors ${
                  status === filter
                    ? 'bg-gray-800 text-white'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800'
                }`}
              >
                {filter}
              </button>
            ))}
          </div>
        </div>
      </div>

      {isError && (
        <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          Failed to load canaries. Is the backend running?
        </div>
      )}

      {!isError && (
        <div className="flex flex-col gap-2 flex-1 min-h-0">
          <div ref={containerRef} className="flex-1 min-h-0 overflow-auto rounded-lg border border-gray-700">
            {!isLoading && (
              <CanaryTable
                canaries={data?.items ?? []}
                emptyText="No canaries found."
                onRowClick={goToDetail}
                canaryTypesByName={canaryTypesByName}
                selectedId={selectedId}
              />
            )}
            {isLoading && <p className="p-4 text-sm text-gray-500">Loading…</p>}
          </div>
          <Pagination
            page={page}
            totalCount={data?.totalCount ?? 0}
            pageSize={pageSize}
            onPageChange={setPage}
          />
        </div>
      )}
    </div>
  )
}
