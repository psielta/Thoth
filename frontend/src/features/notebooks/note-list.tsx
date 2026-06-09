import {
  Archive,
  ArchiveRestore,
  FilePlus2,
  Loader2,
  Pin,
  PinOff,
  Plus,
  Search,
  Trash2,
} from 'lucide-react'
import type * as React from 'react'
import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import type { Note } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useNoteMutations, useNotes } from './use-notes'

type NoteListProps = {
  notebookId: string
  selectedNoteId: string | null
  onSelectNote: (id: string | null) => void
  onCreatePrompt?: (note: Note) => void
}

export function NoteList({ notebookId, selectedNoteId, onSelectNote, onCreatePrompt }: NoteListProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [includeArchived, setIncludeArchived] = useState(false)

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [search])

  const notesQuery = useNotes({
    notebookId,
    q: debouncedSearch || undefined,
    includeArchived,
  })
  const { create, pin, archive, remove } = useNoteMutations()

  const notes = notesQuery.data ?? []

  const handleCreate = () => {
    create.mutate(
      { notebookId, title: 'Nova nota' },
      {
        onSuccess: (note) => onSelectNote(note.id),
        onError: (error) => toast.error(getErrorMessage(error)),
      },
    )
  }

  const handleDelete = (note: Note) => {
    if (!window.confirm(`Excluir a nota "${note.title}"? Esta acao nao pode ser desfeita.`)) {
      return
    }
    remove.mutate(note.id, {
      onSuccess: () => {
        if (selectedNoteId === note.id) {
          onSelectNote(null)
        }
        toast.success('Nota excluida.')
      },
      onError: (error) => toast.error(getErrorMessage(error)),
    })
  }

  return (
    <div className="flex min-h-0 flex-col gap-3 rounded-lg border border-border bg-card p-3">
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar notas"
            className="pl-8"
            aria-label="Buscar notas"
          />
        </div>
        <Button type="button" size="sm" onClick={handleCreate} disabled={create.isPending}>
          <Plus className="h-4 w-4" />
          Nova
        </Button>
      </div>

      <label className="flex items-center gap-2 px-0.5 text-xs text-muted-foreground">
        <input
          type="checkbox"
          checked={includeArchived}
          onChange={(event) => setIncludeArchived(event.target.checked)}
          className="h-3.5 w-3.5 rounded border-input"
        />
        Mostrar arquivadas
      </label>

      <div className="min-h-0 flex-1 space-y-1.5 overflow-y-auto">
        {notesQuery.isLoading ? (
          <div className="flex items-center gap-2 p-3 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Carregando notas
          </div>
        ) : null}

        {notesQuery.isError ? (
          <div className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
            Nao foi possivel carregar as notas.
          </div>
        ) : null}

        {!notesQuery.isLoading && !notesQuery.isError && notes.length === 0 ? (
          <div className="rounded-md border border-dashed border-input p-4 text-center text-sm text-muted-foreground">
            {debouncedSearch ? 'Nenhuma nota encontrada.' : 'Nenhuma nota neste bloco ainda.'}
          </div>
        ) : null}

        {notes.map((note) => (
          <div
            key={note.id}
            role="button"
            tabIndex={0}
            onClick={() => onSelectNote(note.id)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault()
                onSelectNote(note.id)
              }
            }}
            className={cn(
              'group cursor-pointer rounded-md border border-transparent p-2.5 transition-colors hover:bg-accent',
              selectedNoteId === note.id ? 'border-ring bg-accent' : null,
            )}
          >
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                  {note.isPinned ? <Pin className="h-3.5 w-3.5 shrink-0 text-primary" /> : null}
                  <span className="truncate text-sm font-medium text-foreground">{note.title}</span>
                  {note.isArchived ? (
                    <span className="shrink-0 rounded bg-muted px-1.5 py-0.5 text-[0.62rem] text-muted-foreground">
                      Arquivada
                    </span>
                  ) : null}
                </div>
                <p className="mt-0.5 truncate text-xs text-muted-foreground">{preview(note.contentMarkdown)}</p>
              </div>
              <div className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                {onCreatePrompt ? (
                  <IconAction title="Criar prompt a partir da nota" onClick={() => onCreatePrompt(note)}>
                    <FilePlus2 className="h-3.5 w-3.5" />
                  </IconAction>
                ) : null}
                <IconAction
                  title={note.isPinned ? 'Desafixar' : 'Fixar'}
                  onClick={() => pin.mutate({ id: note.id, isPinned: !note.isPinned }, { onError: (error) => toast.error(getErrorMessage(error)) })}
                >
                  {note.isPinned ? <PinOff className="h-3.5 w-3.5" /> : <Pin className="h-3.5 w-3.5" />}
                </IconAction>
                <IconAction
                  title={note.isArchived ? 'Desarquivar' : 'Arquivar'}
                  onClick={() => archive.mutate({ id: note.id, isArchived: !note.isArchived }, { onError: (error) => toast.error(getErrorMessage(error)) })}
                >
                  {note.isArchived ? <ArchiveRestore className="h-3.5 w-3.5" /> : <Archive className="h-3.5 w-3.5" />}
                </IconAction>
                <IconAction title="Excluir" onClick={() => handleDelete(note)} destructive>
                  <Trash2 className="h-3.5 w-3.5" />
                </IconAction>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
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

function preview(content: string): string {
  const text = content
    .replace(/[#>*_`~-]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
  return text.length > 0 ? text.slice(0, 120) : 'Sem conteudo'
}
