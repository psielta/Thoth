import { useQuery } from '@tanstack/react-query'
import {
  Archive,
  ArchiveRestore,
  Loader2,
  NotebookPen,
  Pencil,
  Plus,
  Trash2,
} from 'lucide-react'
import type * as React from 'react'
import { useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { Notebook } from '@/api/schemas'
import { listWorkingDirectories } from '@/api/working-directories'
import { FormField } from '@/components/form-field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { useNotebookMutations, useNotebooks } from './use-notebooks'

type NotebookListProps = {
  selectedNotebookId: string | null
  onSelectNotebook: (id: string | null) => void
}

export function NotebookList({ selectedNotebookId, onSelectNotebook }: NotebookListProps) {
  const [includeArchived, setIncludeArchived] = useState(false)
  const [creating, setCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const notebooksQuery = useNotebooks(includeArchived)
  const { create, update, archive, remove } = useNotebookMutations()
  const notebooks = notebooksQuery.data ?? []

  const closeForm = () => {
    setCreating(false)
    setEditingId(null)
  }

  const handleDelete = (notebook: Notebook) => {
    if (!window.confirm(`Excluir o bloco "${notebook.title}" e todas as suas notas? Esta acao nao pode ser desfeita.`)) {
      return
    }
    remove.mutate(notebook.id, {
      onSuccess: () => {
        if (selectedNotebookId === notebook.id) {
          onSelectNotebook(null)
        }
        toast.success('Bloco excluido.')
      },
      onError: (error) => toast.error(getErrorMessage(error)),
    })
  }

  return (
    <div className="flex min-h-0 flex-col gap-3 rounded-lg border border-border bg-card p-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-foreground">
          <NotebookPen className="h-4 w-4 text-ring" />
          Blocos de notas
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => {
            setEditingId(null)
            setCreating((value) => !value)
          }}
        >
          <Plus className="h-4 w-4" />
          Novo
        </Button>
      </div>

      {creating ? (
        <NotebookForm
          submitting={create.isPending}
          onCancel={closeForm}
          onSubmit={(values) =>
            create.mutate(values, {
              onSuccess: (notebook) => {
                onSelectNotebook(notebook.id)
                closeForm()
                toast.success('Bloco criado.')
              },
              onError: (error) => toast.error(getErrorMessage(error)),
            })
          }
        />
      ) : null}

      <label className="flex items-center gap-2 px-0.5 text-xs text-muted-foreground">
        <input
          type="checkbox"
          checked={includeArchived}
          onChange={(event) => setIncludeArchived(event.target.checked)}
          className="h-3.5 w-3.5 rounded border-input"
        />
        Mostrar arquivados
      </label>

      <div className="min-h-0 flex-1 space-y-1.5 overflow-y-auto">
        {notebooksQuery.isLoading ? (
          <div className="flex items-center gap-2 p-3 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Carregando blocos
          </div>
        ) : null}

        {!notebooksQuery.isLoading && notebooks.length === 0 ? (
          <div className="rounded-md border border-dashed border-input p-4 text-center text-sm text-muted-foreground">
            Nenhum bloco ainda. Crie o primeiro para comecar a anotar.
          </div>
        ) : null}

        {notebooks.map((notebook) =>
          editingId === notebook.id ? (
            <NotebookForm
              key={notebook.id}
              submitting={update.isPending}
              initial={{
                title: notebook.title,
                description: notebook.description ?? '',
                workingDirectoryId: notebook.workingDirectoryId ?? '',
              }}
              onCancel={closeForm}
              onSubmit={(values) =>
                update.mutate(
                  { id: notebook.id, payload: values },
                  {
                    onSuccess: () => {
                      closeForm()
                      toast.success('Bloco atualizado.')
                    },
                    onError: (error) => toast.error(getErrorMessage(error)),
                  },
                )
              }
            />
          ) : (
            <div
              key={notebook.id}
              role="button"
              tabIndex={0}
              onClick={() => onSelectNotebook(notebook.id)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault()
                  onSelectNotebook(notebook.id)
                }
              }}
              className={cn(
                'group cursor-pointer rounded-md border border-transparent p-2.5 transition-colors hover:bg-accent',
                selectedNotebookId === notebook.id ? 'border-ring bg-accent' : null,
              )}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-1.5">
                    <span className="truncate text-sm font-medium text-foreground">{notebook.title}</span>
                    {notebook.isArchived ? (
                      <span className="shrink-0 rounded bg-muted px-1.5 py-0.5 text-[0.62rem] text-muted-foreground">
                        Arquivado
                      </span>
                    ) : null}
                  </div>
                  <div className="mt-0.5 flex items-center gap-2 text-xs text-muted-foreground">
                    <span>
                      {notebook.noteCount} {notebook.noteCount === 1 ? 'nota' : 'notas'}
                    </span>
                    {notebook.workingDirectoryName ? (
                      <span className="truncate">· {notebook.workingDirectoryName}</span>
                    ) : null}
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                  <IconAction
                    title="Editar"
                    onClick={() => {
                      setCreating(false)
                      setEditingId(notebook.id)
                    }}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </IconAction>
                  <IconAction
                    title={notebook.isArchived ? 'Desarquivar' : 'Arquivar'}
                    onClick={() =>
                      archive.mutate(
                        { id: notebook.id, isArchived: !notebook.isArchived },
                        { onError: (error) => toast.error(getErrorMessage(error)) },
                      )
                    }
                  >
                    {notebook.isArchived ? <ArchiveRestore className="h-3.5 w-3.5" /> : <Archive className="h-3.5 w-3.5" />}
                  </IconAction>
                  <IconAction title="Excluir" onClick={() => handleDelete(notebook)} destructive>
                    <Trash2 className="h-3.5 w-3.5" />
                  </IconAction>
                </div>
              </div>
            </div>
          ),
        )}
      </div>
    </div>
  )
}

type NotebookFormValues = {
  title: string
  description: string | null
  workingDirectoryId: string | null
}

function NotebookForm({
  initial,
  submitting,
  onSubmit,
  onCancel,
}: {
  initial?: { title: string; description: string; workingDirectoryId: string }
  submitting: boolean
  onSubmit: (values: NotebookFormValues) => void
  onCancel: () => void
}) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [workingDirectoryId, setWorkingDirectoryId] = useState(initial?.workingDirectoryId ?? '')

  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
  })
  const workspaces = workspacesQuery.data ?? []

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    const trimmed = title.trim()
    if (!trimmed) {
      toast.error('Informe um titulo para o bloco.')
      return
    }
    onSubmit({
      title: trimmed,
      description: description.trim() || null,
      workingDirectoryId: workingDirectoryId || null,
    })
  }

  return (
    <form onSubmit={submit} className="grid gap-2 rounded-md border border-input bg-background p-3">
      <FormField label="Titulo" htmlFor="notebook-title">
        <Input
          id="notebook-title"
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          placeholder="Ex.: Ideias de produto"
          autoFocus
        />
      </FormField>
      <FormField label="Descricao (opcional)" htmlFor="notebook-description">
        <Textarea
          id="notebook-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          placeholder="Para que serve este bloco?"
          className="min-h-16"
        />
      </FormField>
      <FormField label="Diretorio de trabalho (opcional)" htmlFor="notebook-workspace">
        <Select
          id="notebook-workspace"
          value={workingDirectoryId}
          onChange={(event) => setWorkingDirectoryId(event.target.value)}
        >
          <option value="">Global (sem diretorio)</option>
          {workspaces.map((workspace) => (
            <option key={workspace.id} value={workspace.id}>
              {workspace.name}
            </option>
          ))}
        </Select>
      </FormField>
      <div className="flex items-center justify-end gap-2 pt-1">
        <Button type="button" variant="ghost" size="sm" onClick={onCancel}>
          Cancelar
        </Button>
        <Button type="submit" size="sm" disabled={submitting}>
          Salvar
        </Button>
      </div>
    </form>
  )
}

function IconAction({
  title,
  onClick,
  destructive,
  children,
}: {
  title: string
  onClick: () => void
  destructive?: boolean
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      onClick={(event) => {
        event.stopPropagation()
        onClick()
      }}
      className={cn(
        'rounded p-1 text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground',
        destructive ? 'hover:text-destructive' : null,
      )}
    >
      {children}
    </button>
  )
}
