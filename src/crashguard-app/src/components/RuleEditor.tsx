import type { Channel } from '../data/channels'

const OPERATORS = [
  { value: 'eq', label: '=' },
  { value: 'neq', label: '!=' },
  { value: 'gt', label: '>' },
  { value: 'lt', label: '<' },
  { value: 'contains', label: 'contains' },
  { value: 'exists', label: 'exists' },
] as const

const SEVERITIES = [
  { value: 'info', label: 'Info' },
  { value: 'warning', label: 'Warning' },
  { value: 'critical', label: 'Critical' },
] as const

export type RuleOperator = (typeof OPERATORS)[number]['value']
export type RuleSeverity = (typeof SEVERITIES)[number]['value']

export interface Rule {
  id: string
  field: string
  operator: RuleOperator
  value: string
  severity: RuleSeverity
  channel: string
}

export function newRule(): Rule {
  return {
    id: crypto.randomUUID(),
    field: '',
    operator: 'eq',
    value: '',
    severity: 'warning',
    channel: '',
  }
}

const selectClass =
  'rounded bg-gray-800 border border-gray-700 px-2 py-2 text-sm text-white focus:outline-none focus:border-green-500'
const inputClass =
  'rounded bg-gray-800 border border-gray-700 px-3 py-2 text-sm text-white font-mono placeholder:text-gray-500 focus:outline-none focus:border-green-500'

interface RuleEditorProps {
  rules: Rule[]
  onChange: (rules: Rule[]) => void
  channels: Channel[]
}

export default function RuleEditor({ rules, onChange, channels }: RuleEditorProps) {
  function updateRule(id: string, patch: Partial<Rule>) {
    onChange(rules.map(r => (r.id === id ? { ...r, ...patch } : r)))
  }

  function removeRule(id: string) {
    onChange(rules.filter(r => r.id !== id))
  }

  function addRule() {
    onChange([...rules, newRule()])
  }

  return (
    <div>
      <label className="block text-sm font-medium text-gray-300">
        Rules <span className="text-gray-500">(optional)</span>
      </label>
      <p className="mt-1.5 text-xs text-gray-500">
        Route alerts based on canary metadata. Rules are evaluated independently — every match fires.
      </p>

      {rules.length > 0 && (
        <div className="mt-3 flex flex-col gap-2">
          {rules.map(rule => (
            <div
              key={rule.id}
              className="flex flex-col gap-2 rounded border border-gray-700 bg-gray-800/40 p-3"
            >
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs font-medium text-gray-500 w-10">IF</span>
                <input
                  type="text"
                  value={rule.field}
                  onChange={e => updateRule(rule.id, { field: e.target.value })}
                  placeholder="metadata.orderTotal"
                  className={`${inputClass} flex-1 min-w-[160px]`}
                />
                <select
                  value={rule.operator}
                  onChange={e => updateRule(rule.id, { operator: e.target.value as RuleOperator })}
                  className={selectClass}
                >
                  {OPERATORS.map(op => (
                    <option key={op.value} value={op.value}>
                      {op.label}
                    </option>
                  ))}
                </select>
                {rule.operator !== 'exists' && (
                  <input
                    type="text"
                    value={rule.value}
                    onChange={e => updateRule(rule.id, { value: e.target.value })}
                    placeholder="1000"
                    className={`${inputClass} w-28`}
                  />
                )}
                <button
                  type="button"
                  onClick={() => removeRule(rule.id)}
                  aria-label="Remove rule"
                  className="ml-auto text-gray-500 hover:text-red-400 px-1.5 py-1 text-sm"
                >
                  ✕
                </button>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-xs font-medium text-gray-500 w-10">THEN</span>
                <select
                  value={rule.severity}
                  onChange={e => updateRule(rule.id, { severity: e.target.value as RuleSeverity })}
                  className={`${selectClass} flex-1`}
                >
                  {SEVERITIES.map(s => (
                    <option key={s.value} value={s.value}>
                      {s.label}
                    </option>
                  ))}
                </select>
                <select
                  value={rule.channel}
                  onChange={e => updateRule(rule.id, { channel: e.target.value })}
                  className={`${selectClass} flex-1`}
                >
                  <option value="" disabled>
                    Select a channel
                  </option>
                  {channels.map(c => (
                    <option key={c.id} value={c.name}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          ))}
        </div>
      )}

      <button
        type="button"
        onClick={addRule}
        className="mt-3 px-3 py-1.5 rounded border border-gray-700 text-sm text-gray-300 hover:text-white hover:border-gray-600 transition-colors"
      >
        + Add rule
      </button>
    </div>
  )
}
