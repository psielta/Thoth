import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowRight, Loader2, MessageSquarePlus, X } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { Workflow } from '@/api/schemas'
import { addReviewVerdict, getWorkflow } from '@/api/workflow'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { PhaseBadge } from './badges'
import { REVIEW_TARGET_LABEL, currentPhaseRole, isReviewPhaseRole } from './constants'

type ReviewVerdictDialogProps = {
  promptId: string
  onClose: () => void
}

// Diálogo para lançar o veredito de uma revisão (plano/código). Ao salvar, o veredito vira nota
// na timeline e o fluxo avança para a fase de correção correspondente (decidido no backend).
export function ReviewVerdictDialog({ promptId, onClose }: ReviewVerdictDialogProps) {
  const queryClient = useQueryClient()
  const [verdict, setVerdict] = useState('')

  const workflowQuery = useQuery({
    queryKey: queryKeys.workflow.detail(promptId),
    queryFn: () => getWorkflow(promptId),
  })

  const workflow = workflowQuery.data ?? null
  const role = workflow ? currentPhaseRole(workflow.phases, workflow.currentPhaseId) : null
  const isReview = isReviewPhaseRole(role)
  const targetLabel = isReview ? REVIEW_TARGET_LABEL[role] : null

  const mutation = useMutation({
    mutationFn: () => {
      if (!workflow) {
        throw new Error('Fluxo indisponível. Recarregue e tente novamente.')
      }

      return addReviewVerdict(promptId, verdict.trim(), workflow.rowVersion)
    },
    onSuccess: (updated: Workflow) => {
      queryClient.setQueryData(queryKeys.workflow.detail(promptId), updated)
      void queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
      toast.success('Veredito registrado e fase atualizada.')
      onClose()
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const canSubmit = isReview && verdict.trim().length > 0 && !mutation.isPending

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4 pt-16"
      role="dialog"
      aria-modal="true"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div className="flex w-full max-w-2xl flex-col gap-5 rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted">
              <MessageSquarePlus className="h-4 w-4 text-primary" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-foreground">Adicionar nota de revisão</h2>
              <p className="text-xs text-subtle-foreground">
                O veredito vira nota na timeline e o fluxo avança para a fase de correção correspondente.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted hover:text-foreground"
            aria-label="Fechar"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {workflowQuery.isLoading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Carregando fluxo
          </div>
        ) : !isReview ? (
          <p className="rounded-lg border border-border bg-muted p-3 text-sm text-muted-foreground">
            Esta tarefa não está em uma fase de revisão de plano ou de código. Recarregue o quadro e tente novamente.
          </p>
        ) : (
          <>
            {workflow?.currentPhaseName && targetLabel ? (
              <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                <PhaseBadge name={workflow.currentPhaseName} color={workflow.currentPhaseColor} />
                <ArrowRight className="h-3.5 w-3.5 shrink-0" />
                <span className="rounded-md bg-muted px-2 py-1 font-medium text-foreground">{targetLabel}</span>
              </div>
            ) : null}
            <div className="grid gap-1.5">
              <label className="text-sm font-medium text-foreground" htmlFor="review-verdict">
                Veredito do agente
              </label>
              <Textarea
                id="review-verdict"
                value={verdict}
                onChange={(event) => setVerdict(event.target.value)}
                placeholder="Cole aqui o veredito do Codex/Claude (pontos a corrigir, riscos, aprovação...)"
                rows={10}
                autoFocus
              />
            </div>
          </>
        )}

        <div className="flex items-center justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button type="button" onClick={() => mutation.mutate()} disabled={!canSubmit}>
            {mutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <MessageSquarePlus className="h-4 w-4" />
            )}
            Registrar e avançar
          </Button>
        </div>
      </div>
    </div>
  )
}
