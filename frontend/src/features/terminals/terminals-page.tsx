import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { Loader2, Terminal as TerminalIcon } from 'lucide-react'
import { type ReactNode, useCallback, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { TerminalGroup, TerminalSession } from '@/api/schemas'
import { closeTerminal, getTerminalCapabilities, listAllTerminals } from '@/api/terminals'
import { Button } from '@/components/ui/button'
import {
  TERMINAL_FONT_SIZE_DEFAULT,
  TERMINAL_FONT_SIZE_STORAGE_KEY,
  clampTerminalFontSize,
} from '@/features/prompts/terminal-font-size'
import { useLocalStorage } from '@/hooks/use-local-storage'
import { usePromptHub } from '@/realtime/prompt-hub'
import { PromptTerminalGroup } from './prompt-terminal-group'
import { TerminalViewDrawer } from './terminal-view-drawer'

export function TerminalsPage() {
  const queryClient = useQueryClient()
  const { connected } = usePromptHub()
  const [viewing, setViewing] = useState<{ session: TerminalSession; label: string } | null>(null)

  const [storedFontSize, setStoredFontSize] = useLocalStorage(
    TERMINAL_FONT_SIZE_STORAGE_KEY,
    String(TERMINAL_FONT_SIZE_DEFAULT),
  )
  const fontSize = clampTerminalFontSize(Number.parseInt(storedFontSize, 10))
  const adjustFontSize = useCallback(
    (delta: number) => setStoredFontSize(String(clampTerminalFontSize(fontSize + delta))),
    [fontSize, setStoredFontSize],
  )

  const capabilitiesQuery = useQuery({
    queryKey: queryKeys.terminals.capabilities(),
    queryFn: getTerminalCapabilities,
  })
  const terminalsEnabled = capabilitiesQuery.data?.enabled ?? false

  const groupsQuery = useQuery({
    queryKey: queryKeys.terminals.all(),
    queryFn: listAllTerminals,
    enabled: terminalsEnabled,
    refetchInterval: 5_000,
    refetchOnWindowFocus: true,
  })

  const groups = useMemo(() => groupsQuery.data ?? [], [groupsQuery.data])
  const totalCount = useMemo(
    () => groups.reduce((sum, group) => sum + group.terminals.length, 0),
    [groups],
  )

  const removeSessionFromCache = useCallback(
    (sessionId: string) => {
      queryClient.setQueryData<TerminalGroup[]>(queryKeys.terminals.all(), (current) =>
        (current ?? [])
          .map((group) => ({
            ...group,
            terminals: group.terminals.filter((terminal) => terminal.id !== sessionId),
          }))
          .filter((group) => group.terminals.length > 0),
      )
    },
    [queryClient],
  )

  const addSessionToCache = useCallback(
    (session: TerminalSession) => {
      // A criacao a partir do grupo sempre usa group.promptId (o pai), entao a nova sessao casa
      // pelo promptId e entra como propria. Sessoes de filho nunca chegam por aqui; ainda assim, o
      // invalidateQueries(all()) em handleSessionCreated reconcilia qualquer caso de borda.
      queryClient.setQueryData<TerminalGroup[]>(queryKeys.terminals.all(), (current) =>
        (current ?? []).map((group) =>
          group.promptId === session.promptId
            ? { ...group, terminals: [...group.terminals, session] }
            : group,
        ),
      )
    },
    [queryClient],
  )

  const handleView = useCallback((session: TerminalSession, label: string) => {
    setViewing({ session, label })
  }, [])

  const handleCloseDrawer = useCallback(() => setViewing(null), [])

  const handleSessionExit = useCallback(
    (sessionId: string) => {
      setViewing((current) => (current?.session.id === sessionId ? null : current))
      removeSessionFromCache(sessionId)
    },
    [removeSessionFromCache],
  )

  const closeMutation = useMutation({
    mutationFn: (vars: { sessionId: string; promptId: string }) => closeTerminal(vars.sessionId),
    onMutate: ({ sessionId }) => {
      setViewing((current) => (current?.session.id === sessionId ? null : current))
    },
    onSuccess: (_data, { sessionId, promptId }) => {
      removeSessionFromCache(sessionId)
      void queryClient.invalidateQueries({ queryKey: queryKeys.terminals.all() })
      void queryClient.invalidateQueries({ queryKey: queryKeys.terminals.forPrompt(promptId) })
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const handleCloseSession = useCallback(
    (sessionId: string, promptId: string) => {
      closeMutation.mutate({ sessionId, promptId })
    },
    [closeMutation],
  )

  const handleSessionCreated = useCallback(
    (session: TerminalSession) => {
      addSessionToCache(session)
      void queryClient.invalidateQueries({ queryKey: queryKeys.terminals.all() })
      void queryClient.invalidateQueries({ queryKey: queryKeys.terminals.forPrompt(session.promptId) })
    },
    [addSessionToCache, queryClient],
  )

  const isLoading = capabilitiesQuery.isLoading || (terminalsEnabled && groupsQuery.isLoading)

  return (
    <div className="flex min-h-[32rem] flex-col gap-4 lg:h-[calc(100svh-7rem)]">
      <header className="shrink-0">
        <h1 className="text-2xl font-semibold text-foreground">Terminais</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {terminalsEnabled && totalCount > 0
            ? `${totalCount} ${totalCount === 1 ? 'terminal' : 'terminais'} em ${groups.length} ${
                groups.length === 1 ? 'prompt' : 'prompts'
              }, agrupados por prompt.`
            : 'Todos os terminais em execução, agrupados por prompt.'}
        </p>
      </header>

      {terminalsEnabled && !connected ? (
        <div
          role="status"
          className="shrink-0 rounded-md border border-border bg-card px-3 py-2 text-sm text-muted-foreground"
        >
          Reconectando ao servidor de terminais…
        </div>
      ) : null}

      {isLoading ? (
        <CenteredPane>
          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          Carregando terminais
        </CenteredPane>
      ) : !terminalsEnabled ? (
        <EmptyPane message="Os terminais estão desativados nesta instância." />
      ) : groupsQuery.isError ? (
        <CenteredPane>
          <div className="grid justify-items-center gap-3 text-center">
            <span>{getErrorMessage(groupsQuery.error)}</span>
            <Button type="button" size="sm" variant="secondary" onClick={() => void groupsQuery.refetch()}>
              Tentar novamente
            </Button>
          </div>
        </CenteredPane>
      ) : totalCount === 0 ? (
        <EmptyPane
          message="Nenhum terminal em execução. Abra um terminal a partir de um prompt."
          action={
            <Link to="/workspaces" className="text-sm font-medium text-primary hover:underline">
              Ir para diretórios
            </Link>
          }
        />
      ) : (
        <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-1">
          {groups.map((group) => (
            <PromptTerminalGroup
              key={group.promptId}
              group={group}
              closeDisabled={closeMutation.isPending}
              onView={handleView}
              onCloseSession={handleCloseSession}
              onSessionCreated={handleSessionCreated}
            />
          ))}
        </div>
      )}

      {viewing ? (
        <TerminalViewDrawer
          session={viewing.session}
          label={viewing.label}
          fontSize={fontSize}
          onClose={handleCloseDrawer}
          onSessionExit={handleSessionExit}
          onAdjustFontSize={adjustFontSize}
        />
      ) : null}
    </div>
  )
}

function CenteredPane({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-[20rem] flex-1 items-center justify-center rounded-lg border border-border bg-card p-6 text-sm text-muted-foreground">
      {children}
    </div>
  )
}

function EmptyPane({ message, action }: { message: string; action?: ReactNode }) {
  return (
    <div className="flex min-h-[20rem] flex-1 flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-input bg-card p-6 text-center text-sm text-muted-foreground">
      <TerminalIcon className="h-6 w-6 text-muted-foreground" />
      {message}
      {action}
    </div>
  )
}
