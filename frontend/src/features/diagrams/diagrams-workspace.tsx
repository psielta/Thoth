import { useState } from 'react'
import { DiagramDetailPane } from './diagram-detail-pane'
import { DiagramList } from './diagram-list'

type DiagramsWorkspaceProps = {
  workspaceId: string
}

export function DiagramsWorkspace({ workspaceId }: DiagramsWorkspaceProps) {
  const [selectedDiagramId, setSelectedDiagramId] = useState<string | null>(null)

  return (
    <div className="flex min-h-[36rem] flex-col gap-4 lg:h-[calc(100svh-15rem)]">
      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[20rem_minmax(0,1fr)]">
        <DiagramList
          workspaceId={workspaceId}
          selectedDiagramId={selectedDiagramId}
          onSelect={setSelectedDiagramId}
        />
        <DiagramDetailPane diagramId={selectedDiagramId} />
      </div>
    </div>
  )
}
