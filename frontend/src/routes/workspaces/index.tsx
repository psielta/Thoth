import { createFileRoute } from '@tanstack/react-router'
import { WorkspaceForm } from '@/features/workspaces/workspace-form'
import { WorkspaceList } from '@/features/workspaces/workspace-list'

export const Route = createFileRoute('/workspaces/')({
  component: WorkspacesPage,
})

function WorkspacesPage() {
  return (
    <div className="grid gap-6 lg:grid-cols-[22rem_minmax(0,1fr)]">
      <WorkspaceForm />
      <section className="grid content-start gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-[#172126]">Diretorios de trabalho</h1>
          <p className="mt-1 text-sm text-[#66746b]">
            Escolha uma raiz para criar prompts em markdown com mencoes a arquivos.
          </p>
        </div>
        <WorkspaceList />
      </section>
    </div>
  )
}
