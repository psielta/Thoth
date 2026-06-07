import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { api } from './client'
import { createNote, listNotes, setNotePinned, updateNote } from './notes'

vi.mock('./client', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

const apiMock = api as unknown as {
  get: Mock
  post: Mock
  put: Mock
  delete: Mock
}

function jsonResponse(payload: unknown) {
  return { json: () => Promise.resolve(payload) }
}

const notebookId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c0'
const noteId = '019e9f6a-94e7-7a23-965d-c8b05c63ee59'

const sampleNote = {
  id: noteId,
  notebookId,
  title: 'Investigar SignalR',
  contentMarkdown: '# plano',
  isPinned: false,
  isArchived: false,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: '2026-06-01T00:00:00Z',
}

describe('notes api', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('lists notes with notebook, search and archived filters', async () => {
    apiMock.get.mockReturnValue(jsonResponse([sampleNote]))

    await expect(listNotes({ notebookId, q: 'redis', includeArchived: true })).resolves.toEqual([sampleNote])

    const [path, options] = apiMock.get.mock.calls[0]
    expect(path).toBe('notes')
    expect((options.searchParams as URLSearchParams).toString()).toBe(
      `notebookId=${notebookId}&q=redis&includeArchived=true`,
    )
  })

  it('omits empty filters', async () => {
    apiMock.get.mockReturnValue(jsonResponse([]))

    await listNotes({ notebookId })

    const [, options] = apiMock.get.mock.calls[0]
    expect((options.searchParams as URLSearchParams).toString()).toBe(`notebookId=${notebookId}`)
  })

  it('creates a note', async () => {
    apiMock.post.mockReturnValue(jsonResponse(sampleNote))

    await expect(createNote({ notebookId, title: 'Nova nota' })).resolves.toMatchObject({ id: noteId })
    expect(apiMock.post).toHaveBeenCalledWith('notes', { json: { notebookId, title: 'Nova nota' } })
  })

  it('updates a note', async () => {
    apiMock.put.mockReturnValue(jsonResponse({ ...sampleNote, title: 'Atualizado' }))

    await expect(updateNote(noteId, { title: 'Atualizado', contentMarkdown: 'x' })).resolves.toMatchObject({
      title: 'Atualizado',
    })
    expect(apiMock.put).toHaveBeenCalledWith(`notes/${noteId}`, {
      json: { title: 'Atualizado', contentMarkdown: 'x' },
    })
  })

  it('pins a note', async () => {
    apiMock.post.mockReturnValue(jsonResponse({ ...sampleNote, isPinned: true }))

    await expect(setNotePinned(noteId, true)).resolves.toMatchObject({ isPinned: true })
    expect(apiMock.post).toHaveBeenCalledWith(`notes/${noteId}/pin`, { json: { isPinned: true } })
  })
})
