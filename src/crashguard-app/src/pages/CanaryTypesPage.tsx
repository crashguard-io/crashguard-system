import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  createColumnHelper,
} from '@tanstack/react-table'
import { formatTimeout, type CanarySeverity } from '../data/canaryTypes'
import { listCanaryTypes, deleteCanaryType, type CanaryTypeDto } from '../api/canaryTypes'

const columnHelper = createColumnHelper<CanaryTypeDto>()

const severityStyles: Record<CanarySeverity, string> = {
  info: 'bg-blue-500/20 text-blue-400 border-blue-500/30',
  warning: 'bg-amber-500/20 text-amber-400 border-amber-500/30',
  critical: 'bg-red-500/20 text-red-400 border-red-500/30',
}

const columns = [
  columnHelper.display({
    id: 'select',
    header: '',
    size: 40,
    cell: ({ row, table }) => (
      <input
        type="radio"
        name="canary-type-select"
        checked={row.getIsSelected()}
        onChange={() => { table.resetRowSelection(); row.toggleSelected() }}
        className="accent-green-500"
      />
    ),
  }),
  columnHelper.accessor('name', {
    header: 'Name',
    cell: info => <span className="font-mono text-sm">{info.getValue()}</span>,
  }),
  columnHelper.accessor('timeout', {
    header: 'Timeout',
    cell: info => <span className="font-mono text-sm">{formatTimeout(info.getValue())}</span>,
  }),
  columnHelper.accessor('severity', {
    header: 'Severity',
    cell: info => (
      <span className={`px-2 py-0.5 rounded-full text-xs font-medium capitalize border ${severityStyles[info.getValue()]}`}>
        {info.getValue()}
      </span>
    ),
  }),
  columnHelper.accessor('metadataSchema', {
    header: 'Metadata Schema',
    cell: info => info.getValue()
      ? <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-green-500/20 text-green-400 border border-green-500/30">Defined</span>
      : <span className="text-gray-500 text-xs">None</span>,
  }),
  columnHelper.accessor(row => row.rules.length, {
    id: 'ruleCount',
    header: 'Rules',
    cell: info => info.getValue() > 0
      ? <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-blue-500/20 text-blue-400 border border-blue-500/30">{info.getValue()}</span>
      : <span className="text-gray-500 text-xs">None</span>,
  }),
  columnHelper.accessor('createdAt', {
    header: 'Created At',
    cell: info => new Date(info.getValue()).toLocaleString(),
  }),
]

export default function CanaryTypesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [rowSelection, setRowSelection] = useState({})

  const { data, isLoading, isError } = useQuery({
    queryKey: ['canary-types'],
    queryFn: listCanaryTypes,
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCanaryType,
    onSuccess: () => {
      setRowSelection({})
      queryClient.invalidateQueries({ queryKey: ['canary-types'] })
    },
  })

  const table = useReactTable({
    data: data ?? [],
    columns,
    state: { rowSelection },
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    enableMultiRowSelection: false,
    getRowId: row => String(row.id),
  })

  const selectedRow = table.getSelectedRowModel().rows[0]
  const hasSelection = selectedRow !== undefined

  function handleDelete() {
    if (!selectedRow) return
    if (!window.confirm(`Delete canary type "${selectedRow.original.name}"?`)) return
    deleteMutation.mutate(selectedRow.original.id)
  }

  return (
    <div className="flex flex-col h-full gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-white">Canary Types</h1>
        <div className="flex gap-2">
          <button
            onClick={() => navigate('/canary-types/new')}
            className="px-4 py-2 rounded bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors"
          >
            Add
          </button>
          <button
            disabled={!hasSelection}
            onClick={() => selectedRow && navigate(`/canary-types/${selectedRow.original.id}/edit`)}
            className="px-4 py-2 rounded bg-gray-700 hover:bg-gray-600 text-white text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Edit
          </button>
          <button
            disabled={!hasSelection || deleteMutation.isPending}
            onClick={handleDelete}
            className="px-4 py-2 rounded bg-red-700 hover:bg-red-600 text-white text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Delete
          </button>
        </div>
      </div>

      {isError && (
        <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          Failed to load canary types. Is the backend running?
        </div>
      )}

      <div className="flex-1 overflow-auto rounded-lg border border-gray-700">
        <table className="w-full text-sm text-gray-300 border-collapse">
          <thead className="sticky top-0 bg-gray-800 text-gray-400 text-xs uppercase tracking-wider">
            {table.getHeaderGroups().map(headerGroup => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map(header => (
                  <th
                    key={header.id}
                    className="px-4 py-3 text-left font-medium border-b border-gray-700"
                    style={{ width: header.column.getSize() === 150 ? undefined : header.column.getSize() }}
                  >
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
                onClick={() => { table.resetRowSelection(); row.toggleSelected() }}
                onDoubleClick={() => navigate(`/canary-types/${row.original.id}/edit`)}
                className={`border-b border-gray-700/50 cursor-pointer transition-colors ${row.getIsSelected() ? 'bg-gray-700/60' : 'hover:bg-gray-800/60'}`}
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
        {!isLoading && data?.length === 0 && <p className="p-4 text-sm text-gray-500">No canary types yet.</p>}
      </div>
    </div>
  )
}
