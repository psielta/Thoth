import { createFileRoute } from '@tanstack/react-router'
import { PromptList } from '@/features/prompts/prompt-list'

export const Route = createFileRoute('/workspaces/$workspaceId/')({
  component: WorkspaceIndexPage,
})

function WorkspaceIndexPage() {
  const { workspaceId } = Route.useParams()

  return <PromptList workingDirectoryId={workspaceId} />
}
