import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as notesApi from '@/api/notes'
import type { Note } from '@/api/schemas'
import { NoteList } from './note-list'

vi.mock('@/api/notes')

const sampleNote: Note = {
  id: 'note-1',
  notebookId: 'nb-1',
  title: 'Minha nota',
  contentMarkdown: 'algum conteudo',
  isPinned: false,
  isArchived: false,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
}

function renderList(onSelectNote = vi.fn()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <NoteList notebookId="nb-1" selectedNoteId={null} onSelectNote={onSelectNote} />
    </QueryClientProvider>,
  )

  return { onSelectNote }
}

describe('NoteList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(notesApi.listNotes).mockResolvedValue([sampleNote])
    vi.mocked(notesApi.createNote).mockResolvedValue({ ...sampleNote, id: 'note-2', title: 'Nova nota' })
  })

  afterEach(() => {
    cleanup()
  })

  it('renders notes returned by the API', async () => {
    renderList()

    expect(await screen.findByText('Minha nota')).toBeInTheDocument()
    expect(notesApi.listNotes).toHaveBeenCalledWith({
      notebookId: 'nb-1',
      q: undefined,
      includeArchived: false,
    })
  })

  it('creates a note and selects it', async () => {
    const { onSelectNote } = renderList()
    await screen.findByText('Minha nota')

    await userEvent.click(screen.getByRole('button', { name: 'Nova' }))

    await waitFor(() =>
      expect(notesApi.createNote).toHaveBeenCalledWith({ notebookId: 'nb-1', title: 'Nova nota' }),
    )
    await waitFor(() => expect(onSelectNote).toHaveBeenCalledWith('note-2'))
  })

  it('shows an empty state when there are no notes', async () => {
    vi.mocked(notesApi.listNotes).mockResolvedValue([])
    renderList()

    expect(await screen.findByText('Nenhuma nota neste bloco ainda.')).toBeInTheDocument()
  })
})
