import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  createColumnHelper,
} from '@tanstack/react-table'
import type { Channel } from '../data/channels'
import { connectorRegistry } from '../data/connectors'
import { listChannels, deleteChannel } from '../api/channels'

const columnHelper = createColumnHelper<Channel>()

const columns = [
  columnHelper.display({
    id: 'select',
    header: '',
    size: 40,
    cell: ({ row, table }) => (
      <input
        type="radio"
        name="channel-select"
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
  columnHelper.accessor('type', {
    header: 'Connector',
    cell: info => (
      <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-700 text-gray-300 border border-gray-600">
        {connectorRegistry[info.getValue()].label}
      </span>
    ),
  }),
  columnHelper.accessor('createdAt', {
    header: 'Created At',
    cell: info => new Date(info.getValue()).toLocaleString(),
  }),
]

export default function ChannelsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [rowSelection, setRowSelection] = useState({})

  const { data, isLoading, isError } = useQuery({
    queryKey: ['channels'],
    queryFn: listChannels,
  })

  const deleteMutation = useMutation({
    mutationFn: deleteChannel,
    onSuccess: () => {
      setRowSelection({})
      queryClient.invalidateQueries({ queryKey: ['channels'] })
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
    if (!window.confirm(`Delete channel "${selectedRow.original.name}"?`)) return
    deleteMutation.mutate(selectedRow.original.id)
  }

  return (
    <div className="flex flex-col h-full gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-white">Channels</h1>
        <div className="flex gap-2">
          <button
            onClick={() => navigate('/channels/new')}
            className="px-4 py-2 rounded bg-green-600 hover:bg-green-500 text-white text-sm font-medium transition-colors"
          >
            Add
          </button>
          <button
            disabled={!hasSelection}
            onClick={() => selectedRow && navigate(`/channels/${selectedRow.original.id}/edit`)}
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
          Failed to load channels. Is the backend running?
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
                onDoubleClick={() => navigate(`/channels/${row.original.id}/edit`)}
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
        {!isLoading && data?.length === 0 && <p className="p-4 text-sm text-gray-500">No channels yet.</p>}
      </div>
    </div>
  )
}
