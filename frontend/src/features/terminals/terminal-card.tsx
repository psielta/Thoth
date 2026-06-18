import { Link } from '@tanstack/react-router'
import { ExternalLink, Eye, Terminal as TerminalIcon, X } from 'lucide-react'
import type { TerminalSession } from '@/api/schemas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  resolveTerminalTabLabel,
  type TerminalTabPreference,
} from '@/features/prompts/terminal-tab-preferences'

const createdAtFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

type TerminalCardProps = {
  session: TerminalSession
  index: number
  workspaceId: string
  /** Prompt usado no link "Abrir no prompt"; sempre o prompt do grupo (o pai). */
  linkPromptId: string
  preference?: TerminalTabPreference
  closeDisabled: boolean
  onView: (session: TerminalSession, label: string) => void
  onClose: () => void
}

export function TerminalCard({
  session,
  index,
  workspaceId,
  linkPromptId,
  preference,
  closeDisabled,
  onView,
  onClose,
}: TerminalCardProps) {
  const label = resolveTerminalTabLabel(preference, index)
  const accentColor = preference?.color ?? null
  const shellName = session.shell.split(/[\\/]/).pop() ?? session.shell
  const childTitle = session.ownerPromptTitle ?? null

  return (
    <div
      className="grid min-w-0 grid-cols-[minmax(0,1fr)] content-start gap-3 rounded-lg border border-border bg-card p-3"
      style={accentColor ? { boxShadow: `inset 3px 0 0 0 ${accentColor}` } : undefined}
    >
      <div className="flex min-w-0 items-center gap-2">
        <TerminalIcon className="h-4 w-4 shrink-0 text-muted-foreground" />
        <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground" title={label}>
          {label}
        </span>
        <Badge variant="neutral" className="shrink-0 font-mono" title={session.shell}>
          {shellName}
        </Badge>
      </div>

      {session.isChild ? (
        <Badge
          variant="blue"
          className="min-w-0 max-w-full overflow-hidden"
          title={childTitle ? `Filho: ${childTitle}` : 'Terminal de prompt filho'}
        >
          <span className="min-w-0 truncate">{childTitle ? `Filho: ${childTitle}` : 'Filho'}</span>
        </Badge>
      ) : null}

      <div className="grid min-w-0 gap-1 text-xs text-muted-foreground">
        <span className="truncate font-mono" title={session.cwd}>
          {session.cwd}
        </span>
        <span>Criado em {createdAtFormatter.format(new Date(session.createdAtUtc))}</span>
      </div>

      <div className="flex min-w-0 max-w-full flex-wrap items-center gap-2">
        <Button type="button" size="sm" variant="secondary" onClick={() => onView(session, label)}>
          <Eye className="h-4 w-4" />
          Visualizar
        </Button>
        <Link
          to="/workspaces/$workspaceId/prompts/$promptId"
          params={{ workspaceId, promptId: linkPromptId }}
          search={{ tab: 'terminals' }}
          className="min-w-0"
        >
          <Button type="button" size="sm" variant="ghost">
            <ExternalLink className="h-4 w-4" />
            Abrir no prompt
          </Button>
        </Link>
        <Button
          type="button"
          size="sm"
          variant="ghost"
          className="ml-auto text-muted-foreground"
          aria-label={`Fechar ${label}`}
          disabled={closeDisabled}
          onClick={onClose}
        >
          <X className="h-4 w-4" />
          Fechar
        </Button>
      </div>
    </div>
  )
}
