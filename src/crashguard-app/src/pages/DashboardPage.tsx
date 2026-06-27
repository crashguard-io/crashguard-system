import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getCanarySummary, listCanariesPaged, type CanaryDto, type CanaryStatus } from '../api/canaries'

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

function timeUntil(expiresAt: string) {
  const ms = new Date(expiresAt).getTime() - Date.now()
  const minutes = Math.round(ms / 60000)
  if (minutes <= 0) return 'overdue'
  if (minutes < 60) return `${minutes}m`
  const hours = Math.round(minutes / 60)
  return `${hours}h`
}

function CanaryTable({
  canaries,
  emptyText,
  onRowClick,
  lastColumnLabel = 'Expires In',
  lastColumnValue = canary => timeUntil(canary.expiresAt),
}: {
  canaries: CanaryDto[]
  emptyText: string
  onRowClick?: (canary: CanaryDto) => void
  lastColumnLabel?: string
  lastColumnValue?: (canary: CanaryDto) => string
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
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Status</th>
          <th className="px-4 py-3 text-left font-medium border-b border-gray-700">{lastColumnLabel}</th>
        </tr>
      </thead>
      <tbody>
        {canaries.map(canary => (
          <tr
            key={canary.id}
            onClick={() => onRowClick?.(canary)}
            className={`border-b border-gray-700/50 hover:bg-gray-800/60 transition-colors ${onRowClick ? 'cursor-pointer' : ''}`}
          >
            <td className="px-4 py-3 font-mono text-sm">{canary.canaryType}</td>
            <td className="px-4 py-3 font-mono text-sm text-gray-400">{canary.referenceId}</td>
            <td className="px-4 py-3"><StatusBadge status={canary.status} /></td>
            <td className="px-4 py-3 text-sm">{lastColumnValue(canary)}</td>
          </tr>
        ))}
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

export default function DashboardPage() {
  const navigate = useNavigate()
  const [triggeredPage, setTriggeredPage] = useState(1)
  const triggeredContainerRef = useRef<HTMLDivElement>(null)
  const triggeredPageSize = useRowsThatFit(triggeredContainerRef)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['canary-summary'],
    queryFn: getCanarySummary,
    refetchInterval: 15_000,
  })

  const { data: triggered, isLoading: triggeredLoading, isError: triggeredError } = useQuery({
    queryKey: ['canaries', 'Triggered', triggeredPage, triggeredPageSize],
    queryFn: () => listCanariesPaged('Triggered', triggeredPage, triggeredPageSize),
    refetchInterval: 15_000,
  })

  useEffect(() => {
    if (!triggered) return
    const totalPages = Math.max(1, Math.ceil(triggered.totalCount / triggeredPageSize))
    if (triggeredPage > totalPages) setTriggeredPage(totalPages)
  }, [triggered, triggeredPageSize, triggeredPage])

  function goToDetail(canary: CanaryDto) {
    navigate(`/canaries/${encodeURIComponent(canary.canaryType)}/${encodeURIComponent(canary.referenceId)}`, {
      state: { from: '/' },
    })
  }

  return (
    <div className="flex flex-col h-full gap-6 min-h-0">
      <div>
        <h1 className="text-2xl font-semibold text-white">Dashboard</h1>
        <p className="text-gray-400 mt-1">Overview of your canaries.</p>
      </div>

      {isError && (
        <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          Failed to load dashboard data. Is the backend running?
        </div>
      )}

      {!isError && (
        <>
          <div className="grid grid-cols-3 gap-4 shrink-0">
            <div className="rounded-lg border border-gray-700 bg-gray-800/40 px-5 py-4">
              <p className="text-xs uppercase tracking-wider text-gray-400">Pending</p>
              <p className="text-3xl font-semibold text-blue-400 mt-1">{isLoading ? '—' : data?.pendingCount}</p>
            </div>
            <div className="rounded-lg border border-gray-700 bg-gray-800/40 px-5 py-4">
              <p className="text-xs uppercase tracking-wider text-gray-400">Resolved</p>
              <p className="text-3xl font-semibold text-green-400 mt-1">{isLoading ? '—' : data?.resolvedCount}</p>
            </div>
            <div className="rounded-lg border border-gray-700 bg-gray-800/40 px-5 py-4">
              <p className="text-xs uppercase tracking-wider text-gray-400">Triggered</p>
              <p className="text-3xl font-semibold text-red-400 mt-1">{isLoading ? '—' : data?.triggeredCount}</p>
            </div>
          </div>

          <div className="flex flex-col gap-2 flex-1 min-h-0">
            <h2 className="text-sm font-semibold text-white uppercase tracking-wider">Triggered</h2>
            <div ref={triggeredContainerRef} className="flex-1 min-h-0 overflow-auto rounded-lg border border-gray-700">
              {triggeredError && (
                <p className="p-4 text-sm text-red-400">Failed to load triggered canaries.</p>
              )}
              {!triggeredLoading && !triggeredError && (
                <CanaryTable
                  canaries={triggered?.items ?? []}
                  emptyText="No triggered canaries."
                  onRowClick={goToDetail}
                  lastColumnLabel="Started At"
                  lastColumnValue={canary => new Date(canary.startedAt).toLocaleString()}
                />
              )}
              {triggeredLoading && <p className="p-4 text-sm text-gray-500">Loading…</p>}
            </div>
            <Pagination
              page={triggeredPage}
              totalCount={triggered?.totalCount ?? 0}
              pageSize={triggeredPageSize}
              onPageChange={setTriggeredPage}
            />
          </div>

          <div className="flex flex-col gap-2 flex-1 min-h-0">
            <h2 className="text-sm font-semibold text-white uppercase tracking-wider">At Risk</h2>
            <div className="flex-1 min-h-0 overflow-auto rounded-lg border border-gray-700">
              {!isLoading && (
                <CanaryTable
                  canaries={data?.atRisk ?? []}
                  emptyText="No canaries at risk."
                  onRowClick={goToDetail}
                />
              )}
              {isLoading && <p className="p-4 text-sm text-gray-500">Loading…</p>}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
