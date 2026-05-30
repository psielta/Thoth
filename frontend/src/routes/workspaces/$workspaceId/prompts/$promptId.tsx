import { createFileRoute } from '@tanstack/react-router'
import { PromptForm } from '@/features/prompts/prompt-form'
import { PromptVersions } from '@/features/prompts/prompt-versions'

export const Route = createFileRoute('/workspaces/$workspaceId/prompts/$promptId')({
  component: PromptDetailPage,
})

function PromptDetailPage() {
  const { workspaceId, promptId } = Route.useParams()

  return (
    <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_18rem]">
      <PromptForm workingDirectoryId={workspaceId} promptId={promptId} />
      <PromptVersions promptId={promptId} />
    </div>
  )
}
