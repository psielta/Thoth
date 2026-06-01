import { Link } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { Folder, Loader2 } from 'lucide-react'
import { listWorkingDirectories } from '@/api/working-directories'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'

export function WorkspaceList() {
  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
  })

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
        <Link
          key={workspace.id}
          to="/workspaces/$workspaceId"
          params={{ workspaceId: workspace.id }}
          className="group rounded-lg border border-border bg-card p-4 text-left transition-colors hover:border-ring hover:bg-card"
        >
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-sm font-semibold text-foreground">
                <Folder className="h-4 w-4 shrink-0 text-ring" />
                <span className="truncate">{workspace.name}</span>
              </div>
              <p className="mt-1 truncate text-sm text-muted-foreground">{workspace.absolutePath}</p>
            </div>
            <Button type="button" variant="ghost" size="sm" tabIndex={-1}>
              Abrir
            </Button>
          </div>
        </Link>
      ))}
    </div>
  )
}
