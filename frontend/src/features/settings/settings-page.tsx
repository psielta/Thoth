import { Link } from '@tanstack/react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { getAppSettings, updateAppSettings } from '@/api/app-settings'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import { getWorkflowTemplate, updateWorkflowTemplate, type WorkflowPhaseInput } from '@/api/workflow'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { PhaseEditor } from '@/features/workflow/phase-editor'

export function SettingsPage() {
  const queryClient = useQueryClient()
  const appSettingsQuery = useQuery({
    queryKey: queryKeys.appSettings.current(),
    queryFn: getAppSettings,
  })
  const templateQuery = useQuery({
    queryKey: queryKeys.workflow.template(),
    queryFn: getWorkflowTemplate,
  })

  const appSettingsMutation = useMutation({
    mutationFn: updateAppSettings,
    onSuccess: (settings) => {
      queryClient.setQueryData(queryKeys.appSettings.current(), settings)
      toast.success('Configuracoes gerais atualizadas.')
    },
    onError: (error) => toast.error(getErrorMessage(error)),
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
        <h1 className="text-2xl font-semibold text-foreground">Configuracoes</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Ajustes globais do app e do fluxo de prompts.
        </p>
      </div>

      <section className="grid gap-3">
        <div>
          <h2 className="text-base font-semibold text-foreground">Geral</h2>
          <p className="text-sm text-muted-foreground">Preferencias aplicadas em todos os diretorios.</p>
        </div>
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <h3 className="text-sm font-semibold text-foreground">Perguntar sobre agente em prompt filho</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Ao criar um prompt filho, oferecer abrir um terminal com agente executando o prompt automaticamente.
            </p>
          </div>
          {appSettingsQuery.isLoading || !appSettingsQuery.data ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando
            </div>
          ) : (
            <Switch
              id="show-agent-terminal-offer-after-child-prompt"
              checked={appSettingsQuery.data.showAgentTerminalOfferAfterChildPrompt}
              disabled={appSettingsMutation.isPending}
              onChange={(event) =>
                appSettingsMutation.mutate({
                  showAgentTerminalOfferAfterChildPrompt: event.target.checked,
                })
              }
              label={appSettingsQuery.data.showAgentTerminalOfferAfterChildPrompt ? 'Ativo' : 'Inativo'}
            />
          )}
        </div>
      </section>

      <section className="grid gap-3">
        <div>
          <h2 className="text-base font-semibold text-foreground">Fluxo</h2>
          <p className="text-sm text-muted-foreground">
            Edite as fases padrao e o responsavel de cada uma. Tarefas novas usam este template; tarefas em andamento nao sao alteradas.
          </p>
        </div>
        <div className="rounded-lg border border-border bg-card p-4">
          {templateQuery.isLoading || !templateQuery.data ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
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
      </section>
    </div>
  )
}
