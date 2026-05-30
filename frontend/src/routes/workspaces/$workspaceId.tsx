import { Link, Outlet, createFileRoute } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Loader2, Radio } from 'lucide-react'
import { useEffect } from 'react'
import { getWorkingDirectory } from '@/api/working-directories'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'
import { usePromptHub } from '@/realtime/prompt-hub'

export const Route = createFileRoute('/workspaces/$workspaceId')({
  component: WorkspaceLayout,
})

function WorkspaceLayout() {
  const { workspaceId } = Route.useParams()
  const hub = usePromptHub()
  const workspaceQuery = useQuery({
    queryKey: queryKeys.workingDirectories.detail(workspaceId),
    queryFn: () => getWorkingDirectory(workspaceId),
  })

  useEffect(() => {
    hub.joinWorkingDirectory(workspaceId)
    return () => hub.leaveWorkingDirectory(workspaceId)
  }, [hub, workspaceId])

  return (
    <div className="grid gap-5">
      <div className="flex flex-col gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <Link to="/">
            <Button type="button" variant="ghost" size="sm" className="-ml-2 mb-2">
              <ArrowLeft className="h-4 w-4" />
              Diretorios
            </Button>
          </Link>
          {workspaceQuery.isLoading ? (
            <div className="flex items-center gap-2 text-sm text-[#66746b]">
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando diretorio
            </div>
          ) : (
            <>
              <h1 className="truncate text-2xl font-semibold text-[#172126]">{workspaceQuery.data?.name}</h1>
              <p className="mt-1 truncate text-sm text-[#66746b]">{workspaceQuery.data?.absolutePath}</p>
            </>
          )}
        </div>
        <div className="flex items-center gap-2 rounded-md border border-[#d9dfd5] px-2.5 py-1.5 text-xs text-[#66746b]">
          <Radio className={hub.connected ? 'h-3.5 w-3.5 text-[#1f7a3a]' : 'h-3.5 w-3.5 text-[#b42318]'} />
          {hub.connected ? 'Tempo real ativo' : 'Reconectando'}
        </div>
      </div>
      <Outlet />
    </div>
  )
}
