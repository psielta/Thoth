import { Loader2, Shapes } from 'lucide-react'
import { DiagramEditor } from './diagram-editor'
import { useDiagram } from './use-diagrams'

/**
 * Right-hand pane shared by the workspace tab and the global page: loads the
 * selected diagram and shows the editor, a loading state, or an empty hint.
 */
export function DiagramDetailPane({ diagramId }: { diagramId: string | null }) {
  const diagramQuery = useDiagram(diagramId)

  if (diagramId && diagramQuery.isLoading) {
    return (
      <div className="flex min-h-[20rem] items-center justify-center rounded-lg border border-border bg-card text-sm text-muted-foreground">
        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
        Carregando diagrama
      </div>
    )
  }

  if (diagramId && diagramQuery.data) {
    return <DiagramEditor key={diagramQuery.data.id} diagram={diagramQuery.data} />
  }

  return (
    <div className="flex min-h-[20rem] flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-input bg-card p-6 text-center text-sm text-muted-foreground">
      <Shapes className="h-6 w-6 text-muted-foreground" />
      Selecione um diagrama ou crie um novo (Excalidraw ou Mermaid) para comecar.
    </div>
  )
}
