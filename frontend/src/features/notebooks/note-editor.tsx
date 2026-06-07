import { useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Check, Loader2 } from 'lucide-react'
import type * as React from 'react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { updateNote } from '@/api/notes'
import { queryKeys } from '@/api/query-keys'
import type { Note } from '@/api/schemas'
import { MarkdownEditor } from '@/components/markdown-editor'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

type SaveStatus = 'idle' | 'saving' | 'error'

type NoteEditorProps = {
  note: Note
}

/**
 * Title + Markdown editor for a single note. Persists with a debounced autosave
 * and an explicit "Salvar" button, surfacing the current save state. Must be
 * mounted with a `key={note.id}` so each note gets fresh local state.
 */
export function NoteEditor({ note }: NoteEditorProps) {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState(note.title)
  const [content, setContent] = useState(note.contentMarkdown)
  const [saved, setSaved] = useState({ title: note.title, content: note.contentMarkdown })
  const [status, setStatus] = useState<SaveStatus>('idle')

  const trimmedTitle = title.trim()
  const dirty = title !== saved.title || content !== saved.content

  // Mirror the latest values into a ref for the unmount flush. Refs must not be
  // read or written during render, so keep it in sync from an effect.
  const flushRef = useRef({ title, content, saved })
  useEffect(() => {
    flushRef.current = { title, content, saved }
  }, [content, saved, title])

  const invalidate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.notes.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.notebooks.all })
  }, [queryClient])

  const performSave = useCallback(async () => {
    const current = flushRef.current
    const nextTitle = current.title.trim()
    const nextContent = current.content
    if (!nextTitle) {
      return
    }
    if (nextTitle === current.saved.title && nextContent === current.saved.content) {
      return
    }

    setStatus('saving')
    try {
      const updated = await updateNote(note.id, { title: nextTitle, contentMarkdown: nextContent })
      setSaved({ title: updated.title, content: updated.contentMarkdown })
      setStatus('idle')
      invalidate()
    } catch (error) {
      setStatus('error')
      toast.error(getErrorMessage(error))
    }
  }, [invalidate, note.id])

  // Debounced autosave while editing. Re-runs on every keystroke to reset the timer.
  // The "unsaved" state is derived from `dirty`, so the effect never sets state.
  useEffect(() => {
    if (title === saved.title && content === saved.content) {
      return
    }
    if (!title.trim()) {
      return
    }

    const handle = window.setTimeout(() => {
      void performSave()
    }, 800)

    return () => window.clearTimeout(handle)
  }, [content, performSave, saved, title])

  // Flush pending edits when switching notes / unmounting so nothing is lost.
  useEffect(() => {
    return () => {
      const { title: pendingTitle, content: pendingContent, saved: lastSaved } = flushRef.current
      const nextTitle = pendingTitle.trim()
      if (!nextTitle) {
        return
      }
      if (nextTitle === lastSaved.title && pendingContent === lastSaved.content) {
        return
      }

      void updateNote(note.id, { title: nextTitle, contentMarkdown: pendingContent })
        .then(() => invalidate())
        .catch(() => undefined)
    }
  }, [invalidate, note.id])

  const handleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 's') {
      event.preventDefault()
      void performSave()
    }
  }

  return (
    <div
      className="flex min-h-0 flex-1 flex-col gap-3 rounded-lg border border-border bg-card p-4"
      onKeyDown={handleKeyDown}
    >
      <div className="flex flex-col gap-2">
        <Input
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          placeholder="Titulo da nota"
          className="h-10 text-base font-semibold"
          aria-label="Titulo da nota"
        />
        <div className="flex items-center justify-between gap-3">
          <SaveIndicator status={status} dirty={dirty} hasTitle={Boolean(trimmedTitle)} />
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => void performSave()}
            disabled={!dirty || status === 'saving' || !trimmedTitle}
          >
            Salvar
          </Button>
        </div>
      </div>

      <MarkdownEditor
        value={content}
        onChange={setContent}
        label="Conteudo em Markdown"
        className="min-h-0 flex-1"
      />
    </div>
  )
}

function SaveIndicator({ status, dirty, hasTitle }: { status: SaveStatus; dirty: boolean; hasTitle: boolean }) {
  if (!hasTitle) {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs text-warning-foreground">
        <AlertCircle className="h-3.5 w-3.5" />
        Informe um titulo para salvar
      </span>
    )
  }

  if (status === 'saving') {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
        <Loader2 className="h-3.5 w-3.5 animate-spin" />
        Salvando
      </span>
    )
  }

  if (status === 'error') {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs text-destructive">
        <AlertCircle className="h-3.5 w-3.5" />
        Erro ao salvar
      </span>
    )
  }

  if (dirty) {
    return <span className="text-xs text-muted-foreground">Alteracoes nao salvas</span>
  }

  return (
    <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
      <Check className="h-3.5 w-3.5 text-primary" />
      Salvo
    </span>
  )
}
