import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, Save, X } from 'lucide-react'
import { useEffect } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { createFutureTask, updateFutureTask } from '@/api/future-tasks'
import { queryKeys } from '@/api/query-keys'
import { type FutureTask } from '@/api/schemas'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { useFileViewer } from '@/features/files/use-file-viewer'
import { PromptEditor } from '@/features/prompts/prompt-editor'
import { cn } from '@/lib/utils'
import { LABEL_OPTIONS, TYPE_OPTIONS, futureTaskFormSchema, type FutureTaskFormValues } from './constants'

type FutureTaskFormDrawerProps = {
  workspaceId: string
  task?: FutureTask
  onClose: () => void
}

export function FutureTaskFormDrawer({ workspaceId, task, onClose }: FutureTaskFormDrawerProps) {
  const queryClient = useQueryClient()
  const { openFile } = useFileViewer()
  const isEditing = Boolean(task)

  const form = useForm<FutureTaskFormValues>({
    resolver: zodResolver(futureTaskFormSchema),
    defaultValues: {
      title: task?.title ?? '',
      description: task?.description ?? '',
      type: task?.type ?? 'Task',
      labels: task?.labels ?? [],
      issueGithubId: task?.issueGithubId ?? '',
    },
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

  const selectedLabels = useWatch({ control: form.control, name: 'labels' }) ?? []
  const description = useWatch({ control: form.control, name: 'description' }) ?? ''

  const toggleLabel = (label: string) => {
    const next = selectedLabels.includes(label)
      ? selectedLabels.filter((item) => item !== label)
      : [...selectedLabels, label]
    form.setValue('labels', next, { shouldDirty: true, shouldValidate: true })
  }

  const saveMutation = useMutation({
    mutationFn: (values: FutureTaskFormValues) => {
      const issueGithubId = values.issueGithubId.trim() ? values.issueGithubId.trim() : null
      if (task) {
        return updateFutureTask(task.id, {
          title: values.title,
          description: values.description,
          type: values.type,
          labels: values.labels,
          issueGithubId,
          rowVersion: task.rowVersion,
        })
      }

      return createFutureTask({
        workingDirectoryId: workspaceId,
        title: values.title,
        description: values.description,
        type: values.type,
        labels: values.labels,
        issueGithubId,
      })
    },
    onSuccess: async (saved) => {
      queryClient.setQueryData(queryKeys.futureTasks.detail(saved.id), saved)
      await queryClient.invalidateQueries({ queryKey: queryKeys.futureTasks.all })
      toast.success(isEditing ? 'Tarefa atualizada.' : 'Tarefa criada.')
      onClose()
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="future-task-drawer-title"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <form
        onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
        className="grid h-full w-full max-w-[min(96vw,64rem)] grid-rows-[auto_minmax(0,1fr)_auto] border-l border-border bg-card shadow-2xl"
      >
        <div className="flex min-w-0 items-center justify-between gap-3 border-b border-border px-4 py-2.5">
          <h2 id="future-task-drawer-title" className="min-w-0 truncate text-base font-semibold text-foreground">
            {isEditing ? 'Editar tarefa futura' : 'Nova tarefa futura'}
          </h2>
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

        <div className="min-h-0 overflow-auto px-4 py-3">
          <div className="grid gap-4">
            <FormField label="Titulo" htmlFor="future-task-title" error={form.formState.errors.title?.message}>
              <Input
                id="future-task-title"
                placeholder="Suportar tema escuro"
                autoFocus
                {...form.register('title')}
              />
            </FormField>

            <div className="grid gap-3 sm:grid-cols-2">
              <FormField label="Tipo" htmlFor="future-task-type">
                <Select id="future-task-type" {...form.register('type')}>
                  {TYPE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              </FormField>

              <FormField
                label="Issue do GitHub (opcional)"
                htmlFor="future-task-issue"
                error={form.formState.errors.issueGithubId?.message}
              >
                <Input id="future-task-issue" placeholder="Ex.: 42" {...form.register('issueGithubId')} />
              </FormField>
            </div>

            <FormField label="Labels">
              <div className="flex flex-wrap gap-2">
                {LABEL_OPTIONS.map((label) => {
                  const active = selectedLabels.includes(label)
                  return (
                    <button
                      key={label}
                      type="button"
                      onClick={() => toggleLabel(label)}
                      aria-pressed={active}
                      className={cn(
                        'rounded-md border px-2 py-1 text-xs font-medium transition-colors',
                        active
                          ? 'border-primary bg-primary text-primary-foreground'
                          : 'border-border bg-card text-muted-foreground hover:border-ring',
                      )}
                    >
                      {label}
                    </button>
                  )
                })}
              </div>
            </FormField>

            <input type="hidden" {...form.register('description')} />
            <FormField
              label="Descricao (Markdown)"
              error={form.formState.errors.description?.message}
            >
              <PromptEditor
                workingDirectoryId={workspaceId}
                value={description}
                onOpenMention={(relativePath) => openFile(workspaceId, relativePath)}
                onChange={(value) =>
                  form.setValue('description', value, { shouldDirty: true, shouldValidate: true })
                }
                className="grid min-h-[24rem] grid-rows-[auto_minmax(0,1fr)]"
                contentClassName="min-h-0 overflow-auto"
                editorClassName="min-h-[20rem]"
              />
            </FormField>
          </div>
        </div>

        <div className="flex justify-end gap-2 border-t border-border p-4">
          <Button type="button" variant="ghost" onClick={onClose} disabled={saveMutation.isPending}>
            Cancelar
          </Button>
          <Button type="submit" disabled={saveMutation.isPending}>
            {saveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Salvar
          </Button>
        </div>
      </form>
    </div>
  )
}
