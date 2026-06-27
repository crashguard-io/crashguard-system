import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useParams } from 'react-router-dom'
import { getCanary, getCanaryCheckpoints, type CanaryStatus } from '../api/canaries'

function getBackLink(from: string | undefined): { to: string; label: string } {
  if (from?.startsWith('/canaries')) {
    return { to: from, label: 'Back to canaries' }
  }
  return { to: from ?? '/', label: 'Back to dashboard' }
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

function MetadataView({ metadata }: { metadata: unknown }) {
  if (metadata === null || metadata === undefined) {
    return <p className="text-sm text-gray-500">No metadata.</p>
  }

  return (
    <pre className="text-xs text-gray-300 bg-gray-900/60 rounded p-3 overflow-auto whitespace-pre-wrap">
      {JSON.stringify(metadata, null, 2)}
    </pre>
  )
}

export default function CanaryDetailPage() {
  const { canaryType, referenceId } = useParams<{ canaryType: string; referenceId: string }>()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from
  const backLink = getBackLink(from)

  const { data: canary, isLoading, isError } = useQuery({
    queryKey: ['canary', canaryType, referenceId],
    queryFn: () => getCanary(canaryType!, referenceId!),
    enabled: !!canaryType && !!referenceId,
  })

  const { data: checkpoints, isLoading: checkpointsLoading } = useQuery({
    queryKey: ['canary-checkpoints', canaryType, referenceId],
    queryFn: () => getCanaryCheckpoints(canaryType!, referenceId!),
    enabled: !!canaryType && !!referenceId,
  })

  return (
    <div className="flex flex-col gap-6 max-w-3xl mx-auto w-full">
      <div>
        <Link to={backLink.to} className="text-sm text-gray-400 hover:text-gray-300">&larr; {backLink.label}</Link>
        <h1 className="text-2xl font-semibold text-white mt-2 font-mono">
          {canaryType}/{referenceId}
        </h1>
      </div>

      {isError && (
        <div className="rounded border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          Failed to load canary. Is the backend running?
        </div>
      )}

      {isLoading && <p className="text-sm text-gray-500">Loading…</p>}

      {canary && (
        <>
          <div className="rounded-lg border border-gray-700 bg-gray-800/40 p-5 grid grid-cols-2 gap-4">
            <div>
              <p className="text-xs uppercase tracking-wider text-gray-400">Status</p>
              <div className="mt-1.5"><StatusBadge status={canary.status} /></div>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wider text-gray-400">Timeout</p>
              <p className="mt-1.5 text-sm text-gray-300">{canary.timeout}s</p>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wider text-gray-400">Started At</p>
              <p className="mt-1.5 text-sm text-gray-300">{new Date(canary.startedAt).toLocaleString()}</p>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wider text-gray-400">Expires At</p>
              <p className="mt-1.5 text-sm text-gray-300">{new Date(canary.expiresAt).toLocaleString()}</p>
            </div>
            <div>
              <p className="text-xs uppercase tracking-wider text-gray-400">Resolved At</p>
              <p className="mt-1.5 text-sm text-gray-300">
                {canary.resolvedAt ? new Date(canary.resolvedAt).toLocaleString() : '—'}
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <h2 className="text-sm font-semibold text-white uppercase tracking-wider">Metadata</h2>
            <div className="rounded-lg border border-gray-700 bg-gray-800/40 p-3">
              <MetadataView metadata={canary.metadata} />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <h2 className="text-sm font-semibold text-white uppercase tracking-wider">Checkpoints</h2>
            <div className="rounded-lg border border-gray-700 overflow-hidden">
              {checkpointsLoading && <p className="p-4 text-sm text-gray-500">Loading…</p>}
              {!checkpointsLoading && (checkpoints?.length ?? 0) === 0 && (
                <p className="p-4 text-sm text-gray-500">No checkpoints recorded.</p>
              )}
              {!checkpointsLoading && (checkpoints?.length ?? 0) > 0 && (
                <table className="w-full text-sm text-gray-300 border-collapse">
                  <thead className="bg-gray-800 text-gray-400 text-xs uppercase tracking-wider">
                    <tr>
                      <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Stage</th>
                      <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Recorded At</th>
                      <th className="px-4 py-3 text-left font-medium border-b border-gray-700">Metadata</th>
                    </tr>
                  </thead>
                  <tbody>
                    {checkpoints!.map(checkpoint => (
                      <tr key={checkpoint.id} className="border-b border-gray-700/50">
                        <td className="px-4 py-3 font-mono text-sm">{checkpoint.stage}</td>
                        <td className="px-4 py-3 text-sm text-gray-400">
                          {new Date(checkpoint.recordedAt).toLocaleString()}
                        </td>
                        <td className="px-4 py-3">
                          <MetadataView metadata={checkpoint.metadata} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
