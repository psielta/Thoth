import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Bot, MessageSquare, Plus, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import { deleteChatSession, getChatSession, listChatSessions } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'
import { type AiChatSession } from '@/api/schemas'
import { getErrorMessage } from '@/api/client'

type AiSessionListProps = {
  promptId?: string
  workingDirectoryId?: string
  activeSessionId?: string
  onSelectSession: (session: AiChatSession) => void
  onNewSession: () => void
}

export function AiSessionList({
  promptId,
  workingDirectoryId,
  activeSessionId,
  onSelectSession,
  onNewSession,
}: AiSessionListProps) {
  const queryClient = useQueryClient()

  const sessionsQuery = useQuery({
    queryKey: queryKeys.ai.sessions(promptId, workingDirectoryId),
    queryFn: () => listChatSessions({ promptId, workingDirectoryId }),
  })

  const loadSessionMutation = useMutation({
    mutationFn: (id: string) => getChatSession(id),
    onSuccess: (session) => {
      onSelectSession(session)
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  })

  const deleteSessionMutation = useMutation({
    mutationFn: (id: string) => deleteChatSession(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.ai.sessions(promptId, workingDirectoryId) })
      toast.success('Sessao removida.')
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  })

  const sessions = sessionsQuery.data ?? []

  return (
    <div className="flex h-full flex-col">
      {/* Toolbar */}
      <div className="flex items-center justify-between border-b border-secondary px-4 py-3">
        <span className="text-sm font-medium text-foreground">
          {sessions.length === 0 ? 'Nenhuma sessao' : `${sessions.length} sessao${sessions.length > 1 ? 'es' : ''}`}
        </span>
        <button
          onClick={onNewSession}
          className="flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground transition-colors hover:bg-primary-hover"
        >
          <Plus className="h-3.5 w-3.5" />
          Nova sessao
        </button>
      </div>

      {/* List */}
      <div className="flex-1 overflow-y-auto p-3">
        {sessionsQuery.isLoading ? (
          <div className="flex items-center justify-center py-12 text-sm text-subtle-foreground">
            Carregando...
          </div>
        ) : sessions.length === 0 ? (
          <EmptyHistory />
        ) : (
          <div className="flex flex-col gap-1">
            {sessions.map((s) => (
              <SessionItem
                key={s.id}
                session={s}
                isActive={s.id === activeSessionId}
                isLoading={loadSessionMutation.isPending && loadSessionMutation.variables === s.id}
                isDeleting={deleteSessionMutation.isPending && deleteSessionMutation.variables === s.id}
                onSelect={() => loadSessionMutation.mutate(s.id)}
                onDelete={() => deleteSessionMutation.mutate(s.id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function SessionItem({
  session,
  isActive,
  isLoading,
  isDeleting,
  onSelect,
  onDelete,
}: {
  session: AiChatSession
  isActive: boolean
  isLoading: boolean
  isDeleting: boolean
  onSelect: () => void
  onDelete: () => void
}) {
  const date = new Date(session.createdAtUtc)
  const formatted = date.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })

  return (
    <div
      className={`group flex cursor-pointer items-start gap-3 rounded-lg px-3 py-2.5 transition-colors ${
        isActive
          ? 'bg-muted ring-1 ring-primary/20'
          : 'hover:bg-background'
      }`}
      onClick={onSelect}
    >
      {/* Icon */}
      <div className={`mt-0.5 flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-full ${isActive ? 'bg-primary' : 'bg-secondary'}`}>
        <MessageSquare className={`h-3.5 w-3.5 ${isActive ? 'text-primary-foreground' : 'text-muted-foreground'}`} />
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <p className={`truncate text-sm font-medium ${isActive ? 'text-foreground' : 'text-foreground'}`}>
          {isLoading ? 'Carregando...' : session.title}
        </p>
        <div className="mt-0.5 flex items-center gap-2">
          <span className="truncate text-xs text-subtle-foreground">{session.model}</span>
          <span className="text-border">·</span>
          <span className="flex-shrink-0 text-xs text-subtle-foreground">{formatted}</span>
        </div>
      </div>

      {/* Delete */}
      <button
        onClick={(e) => { e.stopPropagation(); onDelete() }}
        disabled={isDeleting}
        className="ml-1 flex-shrink-0 rounded-md p-1 text-transparent transition-colors group-hover:text-subtle-foreground hover:!text-destructive"
        title="Remover sessao"
      >
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}

function EmptyHistory() {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-background">
        <Bot className="h-6 w-6 text-border" />
      </div>
      <div>
        <p className="text-sm font-medium text-muted-foreground">Nenhuma conversa ainda</p>
        <p className="mt-1 text-xs text-subtle-foreground">Inicie uma nova sessao no Chat.</p>
      </div>
    </div>
  )
}
