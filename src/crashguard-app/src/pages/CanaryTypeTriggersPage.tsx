import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  createColumnHelper,
} from '@tanstack/react-table'
import { listCanaryTypeCanaries } from '../api/canaryTypes'
import type { CanaryDto } from '../api/canaries'

const columnHelper = createColumnHelper<CanaryDto>()

const columns = [
  columnHelper.accessor('referenceId', {
    header: 'Reference',
    cell: info => <span className="font-mono text-sm">{info.getValue()}</span>,
  }),
  columnHelper.accessor('triggeredAt', {
    header: 'Triggered At',
    cell: info => info.getValue() ? new Date(info.getValue()!).toLocaleString() : '—',
  }),
  columnHelper.accessor('startedAt', {
    header: 'Started At',
    cell: info => new Date(info.getValue()).toLocaleString(),
  }),
  columnHelper.accessor('timeout', {
    header: 'Timeout',
    cell: info => `${info.getValue()}s`,
  }),
]

export default function CanaryTypeTriggersPage() {
  const { canaryType } = useParams<{ canaryType: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const since = searchParams.get('since') ?? undefined
  const until = searchParams.get('until') ?? undefined

  const { data, isLoading, isError } = useQuery({
    queryKey: ['canary-type-triggers', canaryType, since, until],
    queryFn: () => listCanaryTypeCanaries(canaryType!, 'Triggered', 50, since, until),
    enabled: !!canaryType,
    refetchInterval: 10_000,
  })

  const table = useReactTable({
    data: data ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  return (
    <div className="flex flex-col h-full gap-4">
      <div>
        <Link to="/" className="text-sm text-gray-400 hover:text-gray-300">&larr; Back to dashboard</Link>
        <h1 className="text-2xl font-semibold text-white mt-2 font-mono">{canaryType} &mdash; Recent Triggers</h1>
        {since && until && (
          <p className="mt-1 text-sm text-gray-400">
            Showing triggers from {new Date(since).toLocaleString()} to {new Date(until).toLocaleString()}
          </p>
        )}
        {since && !until && (
          <p className="mt-1 text-sm text-gray-400">Showing triggers since {new Date(since).toLocaleString()}</p>
        )}
      </div>

      {isError && (
        <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          Failed to load triggered canaries. Is the backend running?
        </div>
      )}

      <div className="flex-1 overflow-auto rounded-lg border border-gray-700">
        <table className="w-full text-sm text-gray-300 border-collapse">
          <thead className="sticky top-0 bg-gray-800 text-gray-400 text-xs uppercase tracking-wider">
            {table.getHeaderGroups().map(headerGroup => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map(header => (
                  <th key={header.id} className="px-4 py-3 text-left font-medium border-b border-gray-700">
                    {flexRender(header.column.columnDef.header, header.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {!isLoading && table.getRowModel().rows.map(row => (
              <tr
                key={row.id}
                onDoubleClick={() => navigate(`/canaries/${row.original.canaryType}/${row.original.referenceId}`)}
                className="border-b border-gray-700/50 cursor-pointer transition-colors hover:bg-gray-800/60"
              >
                {row.getVisibleCells().map(cell => (
                  <td key={cell.id} className="px-4 py-3">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
        {isLoading && <p className="p-4 text-sm text-gray-500">Loading…</p>}
        {!isLoading && data?.length === 0 && <p className="p-4 text-sm text-gray-500">No triggered canaries for this type.</p>}
      </div>
    </div>
  )
}
