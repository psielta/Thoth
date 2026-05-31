import { createFileRoute, Link } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import { getWorkflowTemplate, updateWorkflowTemplate, type WorkflowPhaseInput } from '@/api/workflow'
import { Button } from '@/components/ui/button'
import { PhaseEditor } from '@/features/workflow/phase-editor'

export const Route = createFileRoute('/settings')({
  component: SettingsPage,
})

function SettingsPage() {
  const queryClient = useQueryClient()
  const templateQuery = useQuery({
    queryKey: queryKeys.workflow.template(),
    queryFn: getWorkflowTemplate,
  })

  const mutation = useMutation({
    mutationFn: (phases: WorkflowPhaseInput[]) => updateWorkflowTemplate(phases),
    onSuccess: (template) => {
      queryClient.setQueryData(queryKeys.workflow.template(), template)
      toast.success('Template de fases atualizado.')
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  return (
    <div className="grid gap-4">
      <div>
        <Link to="/">
          <Button type="button" variant="ghost" size="sm" className="-ml-2 mb-2">
            <ArrowLeft className="h-4 w-4" />
            Quadro
          </Button>
        </Link>
        <h1 className="text-2xl font-semibold text-[#172126]">Configurações do fluxo</h1>
        <p className="mt-1 text-sm text-[#66746b]">
          Edite as fases padrão e o responsável de cada uma. Tarefas novas usam este template; tarefas em andamento não são alteradas.
        </p>
      </div>

      <div className="rounded-lg border border-[#d9dfd5] bg-white p-4">
        {templateQuery.isLoading || !templateQuery.data ? (
          <div className="flex items-center gap-2 text-sm text-[#66746b]">
            <Loader2 className="h-4 w-4 animate-spin" />
            Carregando template
          </div>
        ) : (
          <PhaseEditor
            key={templateQuery.data.id}
            initialPhases={templateQuery.data.phases.map((phase) => ({
              id: phase.id,
              name: phase.name,
              defaultActor: phase.defaultActor,
              color: phase.color,
            }))}
            saving={mutation.isPending}
            onSave={(phases) => mutation.mutate(phases)}
          />
        )}
      </div>
    </div>
  )
}
