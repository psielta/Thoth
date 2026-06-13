import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Terminal as TerminalIcon, X } from 'lucide-react'
import { useCallback, useMemo, useState } from 'react'
import { closeTerminal, createTerminal, listTerminals } from '@/api/terminals'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/api/client'
import { toast } from 'sonner'
import { TerminalView } from './terminal-view'

type TerminalsPanelProps = {
  promptId: string
}

export function TerminalsPanel({ promptId }: TerminalsPanelProps) {
  const queryClient = useQueryClient()
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null)

  const terminalsQuery = useQuery({
    queryKey: queryKeys.terminals.forPrompt(promptId),
    queryFn: () => listTerminals(promptId),
  })

  const sessions = useMemo(() => terminalsQuery.data ?? [], [terminalsQuery.data])

  const resolvedActiveId = useMemo(() => {
    if (activeSessionId && sessions.some((session) => session.id === activeSessionId)) {
      return activeSessionId
    }
    return sessions[0]?.id ?? null
  }, [activeSessionId, sessions])

  const createMutation = useMutation({
    mutationFn: () => createTerminal(promptId),
    onSuccess: (session) => {
      queryClient.setQueryData(queryKeys.terminals.forPrompt(promptId), (current: typeof sessions | undefined) => [
        ...(current ?? []),
        session,
      ])
      setActiveSessionId(session.id)
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const removeSession = useCallback(
    (sessionId: string) => {
      queryClient.setQueryData(queryKeys.terminals.forPrompt(promptId), (current: typeof sessions | undefined) =>
        (current ?? []).filter((session) => session.id !== sessionId),
      )
      if (resolvedActiveId === sessionId) {
        setActiveSessionId(null)
      }
    },
    [promptId, queryClient, resolvedActiveId],
  )

  const closeMutation = useMutation({
    mutationFn: (sessionId: string) => closeTerminal(sessionId),
    onSuccess: (_, sessionId) => {
      queryClient.setQueryData(queryKeys.terminals.forPrompt(promptId), (current: typeof sessions | undefined) =>
        (current ?? []).filter((session) => session.id !== sessionId),
      )
      if (resolvedActiveId === sessionId) {
        setActiveSessionId(null)
      }
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  return (
    <div className="grid gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" size="sm" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
          <Plus className="h-4 w-4" />
          Novo terminal
        </Button>
        {sessions.length === 0 ? (
          <span className="text-sm text-muted-foreground">Nenhum terminal aberto.</span>
        ) : (
          sessions.map((session, index) => {
            const isActive = session.id === resolvedActiveId
            return (
              <div key={session.id} className="flex items-center gap-1">
                <Button
                  type="button"
                  size="sm"
                  variant={isActive ? 'default' : 'secondary'}
                  onClick={() => setActiveSessionId(session.id)}
                >
                  <TerminalIcon className="h-4 w-4" />
                  Terminal {index + 1}
                </Button>
                <Button
                  type="button"
                  size="icon"
                  variant="ghost"
                  aria-label={`Fechar terminal ${index + 1}`}
                  onClick={() => closeMutation.mutate(session.id)}
                  disabled={closeMutation.isPending}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            )
          })
        )}
      </div>

      {sessions.length > 0 ? (
        <div className="relative h-[min(70vh,640px)] w-full overflow-hidden rounded-md border border-border bg-[#0f1117]">
          {sessions.map((session) => (
            <TerminalView
              key={session.id}
              sessionId={session.id}
              active={session.id === resolvedActiveId}
              onSessionExit={removeSession}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}