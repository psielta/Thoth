import { useMutation } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { ChevronDown, ChevronRight, Plus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import type { TerminalAgentLaunch, TerminalGroup, TerminalSession } from '@/api/schemas'
import { createTerminal } from '@/api/terminals'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { TerminalAgentMenu } from '@/features/prompts/terminal-agent-menu'
import {
  defaultPreferenceForAgent,
  resolveTerminalTabLabel,
} from '@/features/prompts/terminal-tab-preferences'
import { useTerminalTabPreferences } from '@/features/prompts/use-terminal-tab-preferences'
import { TerminalCard } from './terminal-card'

type PromptTerminalGroupProps = {
  group: TerminalGroup
  closeDisabled: boolean
  onView: (session: TerminalSession, label: string) => void
  onCloseSession: (sessionId: string, promptId: string) => void
  onSessionCreated: (session: TerminalSession) => void
}

export function PromptTerminalGroup({
  group,
  closeDisabled,
  onView,
  onCloseSession,
  onSessionCreated,
}: PromptTerminalGroupProps) {
  const [collapsed, setCollapsed] = useState(group.isArchived)
  const sessionIds = useMemo(() => group.terminals.map((terminal) => terminal.id), [group.terminals])
  const { preferences, setSessionPreference } = useTerminalTabPreferences(group.promptId, sessionIds)

  const createMutation = useMutation({
    mutationFn: (agentLaunch?: TerminalAgentLaunch) => createTerminal(group.promptId, { agentLaunch }),
    onSuccess: (session, agentLaunch) => {
      const preference = agentLaunch ? defaultPreferenceForAgent(agentLaunch) : undefined
      if (preference) {
        setSessionPreference(session.id, preference)
      }
      onSessionCreated(session)
      // Abre o terminal recem-criado no drawer (preserva o "ver na hora" do fluxo antigo).
      // group.terminals ainda nao inclui a nova sessao, entao o indice de fallback e o length atual.
      onView(session, resolveTerminalTabLabel(preference, group.terminals.length))
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const regionId = `terminal-group-${group.promptId}`
  const terminalCount = group.terminals.length

  return (
    <section className="grid min-w-0 gap-3 rounded-lg border border-border bg-background p-3">
      <header className="flex min-w-0 flex-wrap items-center gap-2">
        <button
          type="button"
          aria-expanded={!collapsed}
          aria-controls={regionId}
          className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          onClick={() => setCollapsed((current) => !current)}
        >
          {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </button>
        <Link
          to="/workspaces/$workspaceId/prompts/$promptId"
          params={{ workspaceId: group.workingDirectoryId, promptId: group.promptId }}
          search={{ tab: 'terminals' }}
          className="min-w-0 max-w-full truncate text-sm font-semibold text-foreground hover:underline"
          title={group.promptTitle}
        >
          {group.promptTitle}
        </Link>
        <Badge variant="neutral" className="shrink-0">
          {terminalCount} {terminalCount === 1 ? 'terminal' : 'terminais'}
        </Badge>
        {group.isArchived ? (
          <Badge variant="amber" className="shrink-0">
            Arquivado
          </Badge>
        ) : null}
        <span className="min-w-0 truncate text-xs text-muted-foreground" title={group.workingDirectoryName}>
          {group.workingDirectoryName}
        </span>

        <div className="ml-auto inline-flex min-w-0 items-stretch">
          <Button
            type="button"
            size="sm"
            className="rounded-r-none"
            disabled={createMutation.isPending}
            onClick={() => createMutation.mutate(undefined)}
          >
            <Plus className="h-4 w-4" />
            Novo terminal
          </Button>
          <TerminalAgentMenu
            disabled={createMutation.isPending}
            onSelectAgent={(agent) => createMutation.mutate(agent)}
          />
        </div>
      </header>

      {collapsed ? null : (
        <div id={regionId} className="grid min-w-0 gap-3 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4">
          {group.terminals.map((session, index) => (
            <TerminalCard
              key={session.id}
              session={session}
              index={index}
              workspaceId={group.workingDirectoryId}
              // "Abrir no prompt" sempre aponta para o prompt do grupo (o pai): clicar em filho nao
              // deve navegar para a rota de edicao do filho.
              linkPromptId={group.promptId}
              preference={preferences[session.id]}
              closeDisabled={closeDisabled}
              onView={onView}
              // Invalida o cache do dono real do terminal (o filho, quando isChild).
              onClose={() => onCloseSession(session.id, session.promptId)}
            />
          ))}
        </div>
      )}
    </section>
  )
}
