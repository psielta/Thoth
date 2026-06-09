import { useQuery } from '@tanstack/react-query'
import { Loader2, NotebookText } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { getNotebook } from '@/api/notebooks'
import { getNote } from '@/api/notes'
import { queryKeys } from '@/api/query-keys'
import type { Note } from '@/api/schemas'
import { listWorkingDirectories } from '@/api/working-directories'
import { NewPromptDrawer } from '@/features/workflow/new-prompt-drawer'
import { NoteEditor } from './note-editor'
import { NoteList } from './note-list'
import { NotebookList } from './notebook-list'
import { buildPromptSeedFromNote, type NotePromptSeed } from './note-prompt-seed'

export function NotebooksPage() {
  const [selectedNotebookId, setSelectedNotebookId] = useState<string | null>(null)
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null)
  const [promptSeed, setPromptSeed] = useState<NotePromptSeed | null>(null)

  const noteQuery = useQuery({
    queryKey: selectedNoteId ? queryKeys.notes.detail(selectedNoteId) : ['notes', 'none'],
    queryFn: () => getNote(selectedNoteId as string),
    enabled: Boolean(selectedNoteId),
  })

  const notebookQuery = useQuery({
    queryKey: selectedNotebookId ? queryKeys.notebooks.detail(selectedNotebookId) : ['notebooks', 'none'],
    queryFn: () => getNotebook(selectedNotebookId as string),
    enabled: Boolean(selectedNotebookId),
  })

  const workspacesQuery = useQuery({
    queryKey: queryKeys.workingDirectories.all,
    queryFn: listWorkingDirectories,
    enabled: Boolean(promptSeed),
  })

  const handleSelectNotebook = (id: string | null) => {
    setSelectedNotebookId(id)
    setSelectedNoteId(null)
  }

  const handleCreatePromptFromNote = (note: Note) => {
    const seed = buildPromptSeedFromNote(note)
    if (seed.truncated) {
      toast.error('A nota excede o limite do prompt; o conteudo foi recortado no limite permitido.')
    }

    setPromptSeed(seed)
  }

  return (
    <div className="flex min-h-[32rem] flex-col gap-4 lg:h-[calc(100svh-7rem)]">
      <header className="shrink-0">
        <h1 className="text-2xl font-semibold text-foreground">Notas</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Blocos de notas em Markdown salvos no banco do app, sem criar arquivos no diretorio de trabalho.
        </p>
      </header>

      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[18rem_20rem_minmax(0,1fr)]">
        <NotebookList selectedNotebookId={selectedNotebookId} onSelectNotebook={handleSelectNotebook} />

        {selectedNotebookId ? (
          <NoteList
            notebookId={selectedNotebookId}
            selectedNoteId={selectedNoteId}
            onSelectNote={setSelectedNoteId}
            onCreatePrompt={handleCreatePromptFromNote}
          />
        ) : (
          <EmptyPane message="Selecione um bloco para ver suas notas." />
        )}

        {selectedNoteId && noteQuery.isLoading ? (
          <div className="flex min-h-[20rem] items-center justify-center rounded-lg border border-border bg-card text-sm text-muted-foreground">
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Carregando nota
          </div>
        ) : selectedNoteId && noteQuery.data ? (
          <NoteEditor
            key={noteQuery.data.id}
            note={noteQuery.data}
            workingDirectoryId={notebookQuery.data?.workingDirectoryId ?? null}
            onNoteCreated={setSelectedNoteId}
          />
        ) : (
          <EmptyPane
            message={
              selectedNotebookId
                ? 'Selecione uma nota ou crie uma nova para comecar a escrever.'
                : 'Suas notas em Markdown aparecem aqui.'
            }
          />
        )}
      </div>

      {promptSeed && workspacesQuery.data ? (
        <NewPromptDrawer
          workspaces={workspacesQuery.data}
          defaultWorkingDirectoryId={notebookQuery.data?.workingDirectoryId ?? undefined}
          initialTitle={promptSeed.title}
          initialContent={promptSeed.content}
          onClose={() => setPromptSeed(null)}
          onCreated={() => setPromptSeed(null)}
        />
      ) : null}
    </div>
  )
}

function EmptyPane({ message }: { message: string }) {
  return (
    <div className="flex min-h-[20rem] flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-input bg-card p-6 text-center text-sm text-muted-foreground">
      <NotebookText className="h-6 w-6 text-muted-foreground" />
      {message}
    </div>
  )
}
