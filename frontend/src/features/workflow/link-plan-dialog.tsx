import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Link2, Loader2, X } from 'lucide-react'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { getErrorMessage } from '@/api/client'
import { linkLinkedDocument } from '@/api/linked-documents'
import { queryKeys } from '@/api/query-keys'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const schema = z.object({
  absolutePath: z.string().trim().min(1, 'Informe o caminho completo do markdown.'),
})

type Values = z.infer<typeof schema>

type LinkPlanDialogProps = {
  promptId: string
  promptTitle: string
  onClose: () => void
}

// Vincula um plano markdown direto do quadro. O backend dispara LinkedDocumentLinked,
// que ja invalida workflow.all no SignalR; aqui tambem invalidamos para refletir na hora.
export function LinkPlanDialog({ promptId, promptTitle, onClose }: LinkPlanDialogProps) {
  const queryClient = useQueryClient()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { absolutePath: '' },
  })

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  const linkMutation = useMutation({
    mutationFn: (values: Values) =>
      linkLinkedDocument(promptId, { absolutePath: values.absolutePath, documentType: 'ClaudeCodePlan' }),
    onSuccess: async (document) => {
      queryClient.setQueryData(queryKeys.linkedDocuments.detail(document.id), document)
      await queryClient.invalidateQueries({ queryKey: queryKeys.linkedDocuments.forPrompt(promptId) })
      await queryClient.invalidateQueries({ queryKey: queryKeys.workflow.all })
      toast.success('Plano vinculado.')
      onClose()
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="link-plan-dialog-title"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div className="grid w-full max-w-lg gap-4 rounded-lg border border-border bg-card p-4 shadow-2xl">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 id="link-plan-dialog-title" className="text-base font-semibold text-foreground">
              Vincular plano
            </h2>
            <p className="mt-1 truncate text-sm text-muted-foreground" title={promptTitle}>
              {promptTitle}
            </p>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-8 w-8 shrink-0 text-muted-foreground"
            onClick={onClose}
            aria-label="Fechar"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={form.handleSubmit((values) => linkMutation.mutate(values))} className="grid gap-3">
          <FormField
            label="Markdown do plano"
            htmlFor="link-plan-path"
            error={form.formState.errors.absolutePath?.message}
          >
            <Input
              id="link-plan-path"
              placeholder="C:\\Users\\psiel\\.claude\\plans\\plano.md"
              autoComplete="off"
              autoFocus
              {...form.register('absolutePath')}
            />
          </FormField>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose} disabled={linkMutation.isPending}>
              Cancelar
            </Button>
            <Button type="submit" disabled={linkMutation.isPending}>
              {linkMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Link2 className="h-4 w-4" />}
              Vincular
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
