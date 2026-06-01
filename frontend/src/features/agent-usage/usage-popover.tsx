import { AlertCircle, Clock3 } from 'lucide-react'
import type { AgentUsageInfo } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import {
  formatPercent,
  formatReset,
  formatTokens,
  getUsageVariant,
  statusLabels,
  windowLabels,
} from './constants'

type UsagePopoverProps = {
  claude: AgentUsageInfo
  codex: AgentUsageInfo
  capturedAtUtc: string
}

export function UsagePopover({ claude, codex, capturedAtUtc }: UsagePopoverProps) {
  return (
    <div className="grid gap-3">
      <div className="flex items-center justify-between gap-3 border-b border-muted pb-2">
        <span className="text-sm font-semibold text-foreground">Limites dos agentes</span>
        <span className="text-[0.68rem] text-muted-foreground">{new Date(capturedAtUtc).toLocaleTimeString('pt-BR')}</span>
      </div>
      <AgentUsageSection info={claude} />
      <AgentUsageSection info={codex} />
    </div>
  )
}

function AgentUsageSection({ info }: { info: AgentUsageInfo }) {
  const statusVariant = info.status === 'Ok' ? 'green' : info.status === 'RateLimited' ? 'red' : 'amber'

  return (
    <section className="grid gap-2">
      <div className="flex items-center justify-between gap-2">
        <div className="min-w-0">
          <div className="truncate text-sm font-semibold text-foreground">{info.agent}</div>
          {info.plan ? <div className="truncate text-xs text-muted-foreground">{info.plan}</div> : null}
        </div>
        <Badge variant={statusVariant}>{statusLabels[info.status]}</Badge>
      </div>

      {info.status !== 'Ok' ? (
        <div className="flex items-start gap-2 rounded-md bg-warning-soft px-2 py-1.5 text-xs text-warning-foreground">
          <AlertCircle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
          <span>{info.statusDetail || statusLabels[info.status]}</span>
        </div>
      ) : null}

      <div className="grid gap-2">
        {info.windows.map((window) => {
          const tokens = formatTokens(window)
          return (
            <div key={`${info.agent}-${window.key}`} className="grid gap-1">
              <div className="flex items-center justify-between gap-2 text-xs">
                <span className="min-w-0 truncate font-medium text-foreground">
                  {windowLabels[window.key] ?? window.label}
                  {window.estimated ? ' estimado' : ''}
                </span>
                <span className="font-semibold text-foreground">{formatPercent(window.usedPercent)}</span>
              </div>
              <Progress value={window.usedPercent} variant={getUsageVariant(window.usedPercent, info.status)} />
              <div className="flex items-center justify-between gap-2 text-[0.68rem] text-muted-foreground">
                <span className="inline-flex items-center gap-1">
                  <Clock3 className="h-3 w-3" />
                  {formatReset(window.resetsAtUtc)}
                </span>
                {tokens ? <span className="truncate">{tokens}</span> : null}
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}
