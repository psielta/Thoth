import { createFileRoute } from '@tanstack/react-router'
import { PromptForm } from '@/features/prompts/prompt-form'

export const Route = createFileRoute('/workspaces/$workspaceId/prompts/new')({
  component: NewPromptPage,
})

function NewPromptPage() {
  const { workspaceId } = Route.useParams()

  return (
    <section className="grid gap-4">
      <div>
        <h2 className="text-xl font-semibold text-[#172126]">Novo prompt</h2>
        <p className="mt-1 text-sm text-[#66746b]">Digite @ no editor para pesquisar arquivos do diretorio.</p>
      </div>
      <PromptForm workingDirectoryId={workspaceId} />
    </section>
  )
}
