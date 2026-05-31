import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { AlertTriangle, Loader2, Save, Send, X } from 'lucide-react'
import { useCallback, useEffect, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { createPrompt } from '@/api/prompts'
import { renderPromptDraft } from '@/api/prompt-templates'
import { queryKeys } from '@/api/query-keys'
import type { Prompt, PromptTemplate } from '@/api/schemas'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import {
  AGENT_OPTIONS,
  KIND_OPTIONS,
  promptFormSchema,
  type PromptFormValues,
} from '@/features/prompts/constants'

type GeneratePromptDrawerProps = {
  linkedDocumentId: string
  template: PromptTemplate
  onClose: () => void
}

type CreateGeneratedPromptPayload = {
  values: PromptFormValues
  openAfterCreate: boolean
}

export function GeneratePromptDrawer({
  linkedDocumentId,
  template,
  onClose,
}: GeneratePromptDrawerProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const titleInputRef = useRef<HTMLInputElement>(null)
  const form = useForm<PromptFormValues>({
    resolver: zodResolver(promptFormSchema),
    defaultValues: {
      title: '',
      targetAgent: template.defaultTargetAgent,
      kind: template.defaultKind,
      status: 'Draft',
      content: '',
    },
  })

  const draftQuery = useQuery({
    queryKey: queryKeys.promptTemplates.draft(linkedDocumentId, template.key),
    queryFn: () => renderPromptDraft(linkedDocumentId, template.key),
    retry: false,
  })

  useEffect(() => {
    if (!draftQuery.data) {
      return
    }

    form.reset({
      title: draftQuery.data.title,
      targetAgent: draftQuery.data.targetAgent,
      kind: draftQuery.data.kind,
      status: 'Draft',
      content: draftQuery.data.content,
    })

    window.setTimeout(() => titleInputRef.current?.focus(), 0)
  }, [draftQuery.data, form])

  const afterSave = async (prompt: Prompt) => {
    queryClient.setQueryData(queryKeys.prompts.detail(prompt.id), prompt)
    await queryClient.invalidateQueries({ queryKey: queryKeys.prompts.all })
    await queryClient.invalidateQueries({ queryKey: queryKeys.prompts.versions(prompt.id) })
  }

  const createMutation = useMutation({
    mutationFn: ({ values }: CreateGeneratedPromptPayload) => {
      if (!draftQuery.data) {
        throw new Error('Rascunho ainda nao foi gerado.')
      }

      return createPrompt({
        workingDirectoryId: draftQuery.data.workingDirectoryId,
        title: values.title,
        content: values.content,
        targetAgent: values.targetAgent,
        kind: values.kind,
        status: 'Draft',
        mentions: [],
      })
    },
    onSuccess: async (prompt, payload) => {
      await afterSave(prompt)
      toast.success('Prompt criado.')

      if (payload.openAfterCreate) {
        await navigate({
          to: '/workspaces/$workspaceId/prompts/$promptId',
          params: { workspaceId: prompt.workingDirectoryId, promptId: prompt.id },
        })
      }

      onClose()
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  const isBusy = createMutation.isPending
  const isDirty = form.formState.isDirty

  const requestClose = useCallback(() => {
    if (isBusy) {
      return
    }

    if (isDirty && !window.confirm('Descartar o prompt gerado?')) {
      return
    }

    onClose()
  }, [isBusy, isDirty, onClose])

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        requestClose()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [requestClose])

  const submit = (openAfterCreate: boolean) =>
    form.handleSubmit((values) => createMutation.mutate({ values, openAfterCreate }))()
  const titleField = form.register('title')

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-[#172126]/35 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="generate-prompt-title"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          requestClose()
        }
      }}
    >
      <div className="grid h-full w-full max-w-xl grid-rows-[auto_minmax(0,1fr)_auto] border-l border-[#d9dfd5] bg-white shadow-2xl">
        <div className="flex min-w-0 items-start justify-between gap-3 border-b border-[#d9dfd5] p-4">
          <div className="min-w-0">
            <h2 id="generate-prompt-title" className="text-base font-semibold text-[#172126]">
              Gerar prompt
            </h2>
            <p className="mt-1 truncate text-sm text-[#66746b]" title={template.description}>
              {template.displayName}
            </p>
          </div>
          <Button type="button" variant="ghost" size="icon" onClick={requestClose} disabled={isBusy} aria-label="Fechar">
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="min-h-0 overflow-auto p-4">
          {draftQuery.isLoading ? (
            <div className="flex items-center gap-2 rounded-md border border-[#d9dfd5] bg-[#f7f8f6] p-3 text-sm text-[#66746b]">
              <Loader2 className="h-4 w-4 animate-spin" />
              Gerando prompt
            </div>
          ) : null}

          {draftQuery.error ? (
            <div className="grid gap-3 rounded-md border border-[#f8b4aa] bg-[#fff3f0] p-3 text-sm text-[#8a241b]">
              <div className="flex items-start gap-2">
                <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{getErrorMessage(draftQuery.error)}</span>
              </div>
              <Button type="button" variant="secondary" size="sm" onClick={() => draftQuery.refetch()}>
                Tentar novamente
              </Button>
            </div>
          ) : null}

          {draftQuery.data ? (
            <form className="grid gap-4">
              <FormField label="Titulo" htmlFor="generated-prompt-title" error={form.formState.errors.title?.message}>
                <Input
                  id="generated-prompt-title"
                  {...titleField}
                  ref={(element) => {
                    titleField.ref(element)
                    titleInputRef.current = element
                  }}
                />
              </FormField>

              <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="Agente" htmlFor="generated-prompt-agent">
                  <Select id="generated-prompt-agent" {...form.register('targetAgent')}>
                    {AGENT_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                </FormField>

                <FormField label="Tipo" htmlFor="generated-prompt-kind">
                  <Select id="generated-prompt-kind" {...form.register('kind')}>
                    {KIND_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </Select>
                </FormField>
              </div>

              <FormField label="Conteudo" htmlFor="generated-prompt-content" error={form.formState.errors.content?.message}>
                <Textarea
                  id="generated-prompt-content"
                  className="min-h-[24rem] resize-y font-mono"
                  {...form.register('content')}
                />
              </FormField>
            </form>
          ) : null}
        </div>

        <div className="flex flex-wrap justify-end gap-2 border-t border-[#d9dfd5] p-4">
          <Button type="button" variant="ghost" onClick={requestClose} disabled={isBusy}>
            Cancelar
          </Button>
          <Button type="button" variant="secondary" onClick={() => submit(false)} disabled={!draftQuery.data || isBusy}>
            {createMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Criar
          </Button>
          <Button type="button" onClick={() => submit(true)} disabled={!draftQuery.data || isBusy}>
            {createMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            Criar e abrir
          </Button>
        </div>
      </div>
    </div>
  )
}
