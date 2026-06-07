import { api } from './client'
import { type NoteFilters } from './query-keys'
import { noteListSchema, noteSchema, type Note } from './schemas'

export type CreateNotePayload = {
  notebookId: string
  title: string
  contentMarkdown?: string
}

export type UpdateNotePayload = {
  title: string
  contentMarkdown: string
}

export async function listNotes(filters: NoteFilters): Promise<Note[]> {
  const searchParams = new URLSearchParams()
  if (filters.notebookId) {
    searchParams.set('notebookId', filters.notebookId)
  }
  if (filters.q) {
    searchParams.set('q', filters.q)
  }
  if (filters.includeArchived) {
    searchParams.set('includeArchived', 'true')
  }

  const data = await api.get('notes', { searchParams }).json<unknown>()
  return noteListSchema.parse(data)
}

export async function getNote(id: string): Promise<Note> {
  const data = await api.get(`notes/${id}`).json<unknown>()
  return noteSchema.parse(data)
}

export async function createNote(payload: CreateNotePayload): Promise<Note> {
  const data = await api.post('notes', { json: payload }).json<unknown>()
  return noteSchema.parse(data)
}

export async function updateNote(id: string, payload: UpdateNotePayload): Promise<Note> {
  const data = await api.put(`notes/${id}`, { json: payload }).json<unknown>()
  return noteSchema.parse(data)
}

export async function setNotePinned(id: string, isPinned: boolean): Promise<Note> {
  const data = await api.post(`notes/${id}/pin`, { json: { isPinned } }).json<unknown>()
  return noteSchema.parse(data)
}

export async function setNoteArchived(id: string, isArchived: boolean): Promise<Note> {
  const data = await api.post(`notes/${id}/archive`, { json: { isArchived } }).json<unknown>()
  return noteSchema.parse(data)
}

export async function deleteNote(id: string): Promise<void> {
  await api.delete(`notes/${id}`)
}
