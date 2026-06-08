import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { queryKeys } from '@/api/query-keys'
import { listWorkingDirectories } from '@/api/working-directories'
import { DiagramDetailPane } from './diagram-detail-pane'
import { DiagramList } from './diagram-list'

/**
 * Global diagrams page (header route `/diagramas`): lists the user's diagrams
 * across every workspace, with a workspace filter and create-target picker.
 */
export function DiagramsPage() {
  const [selectedDiagramId, setSelectedDiagramId] = useState<string | null>(null)
  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
  })

  return (
    <div className="flex min-h-[36rem] flex-col gap-4 lg:h-[calc(100svh-9rem)]">
      <header className="shrink-0">
        <h1 className="text-2xl font-semibold text-foreground">Diagramas</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Diagramas Excalidraw e Mermaid de todos os seus workspaces, salvos no banco do app.
        </p>
      </header>

      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[22rem_minmax(0,1fr)]">
        <DiagramList
          workspaces={workspacesQuery.data ?? []}
          selectedDiagramId={selectedDiagramId}
          onSelect={setSelectedDiagramId}
        />
        <DiagramDetailPane diagramId={selectedDiagramId} />
      </div>
    </div>
  )
}
