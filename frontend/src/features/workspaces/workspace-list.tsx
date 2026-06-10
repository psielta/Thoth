import { Link } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Folder, Loader2, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { WorkingDirectory } from '@/api/schemas'
import { deleteWorkingDirectory, listWorkingDirectories } from '@/api/working-directories'
import { Button } from '@/components/ui/button'

export function WorkspaceList() {
  const queryClient = useQueryClient()
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
  })
  const deleteWorkspace = useMutation({
    mutationFn: deleteWorkingDirectory,
    onMutate: (id) => setDeletingId(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.workingDirectories.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.futureTasks.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.notebooks.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.notes.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.diagrams.all })
      void queryClient.invalidateQueries({ queryKey: ['ai', 'sessions'] })
      void queryClient.removeQueries({ queryKey: ['files'], exact: false })
      void queryClient.removeQueries({ queryKey: ['git'], exact: false })
      toast.success('Workspace excluido.')
    },
    onError: (error) => toast.error(getErrorMessage(error)),
    onSettled: () => setDeletingId(null),
  })

  const handleDelete = (workspace: WorkingDirectory) => {
    if (
      !window.confirm(
        `Excluir o workspace "${workspace.name}"? Isso remove prompts, tarefas futuras, diagramas, blocos de notas, sessoes de IA e outros dados vinculados no app. A pasta no disco nao sera apagada. Esta acao nao pode ser desfeita.`,
      )
    ) {
      return
    }

    deleteWorkspace.mutate(workspace.id)
  }

  if (workspacesQuery.isLoading) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Carregando diretorios
      </div>
    )
  }

  if (!workspacesQuery.data?.length) {
    return (
      <div className="rounded-lg border border-dashed border-input bg-card p-6 text-sm text-muted-foreground">
        Nenhum diretorio cadastrado.
      </div>
    )
  }

  return (
    <div className="grid gap-2">
      {workspacesQuery.data.map((workspace) => (
        <div
          key={workspace.id}
          className="group flex items-stretch rounded-lg border border-border bg-card transition-colors hover:border-ring"
        >
          <Link
            to="/workspaces/$workspaceId"
            params={{ workspaceId: workspace.id }}
            className="min-w-0 flex-1 p-4 text-left"
          >
            <div className="min-w-0 pr-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                <Folder className="h-4 w-4 shrink-0 text-ring" />
                <span className="truncate">{workspace.name}</span>
              </div>
              <p className="mt-1 truncate text-sm text-muted-foreground">{workspace.absolutePath}</p>
            </div>
          </Link>
          <div className="flex shrink-0 items-center gap-1 p-4 pl-0">
            <Link
              to="/workspaces/$workspaceId"
              params={{ workspaceId: workspace.id }}
              className="inline-flex h-8 items-center justify-center rounded-md border border-transparent px-2.5 text-xs font-medium text-foreground transition-colors hover:bg-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              Abrir
            </Link>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              title="Excluir workspace"
              aria-label={`Excluir workspace ${workspace.name}`}
              disabled={deleteWorkspace.isPending}
              onClick={() => handleDelete(workspace)}
              className="h-8 w-8 text-muted-foreground hover:text-destructive"
            >
              {deletingId === workspace.id && deleteWorkspace.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4" />
              )}
            </Button>
          </div>
        </div>
      ))}
    </div>
  )
}
