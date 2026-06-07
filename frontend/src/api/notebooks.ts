import { api } from './client'
import { notebookListSchema, notebookSchema, type Notebook } from './schemas'

export type NotebookPayload = {
  title: string
  description?: string | null
  workingDirectoryId?: string | null
}

export async function listNotebooks(includeArchived = false): Promise<Notebook[]> {
  const searchParams = new URLSearchParams()
  if (includeArchived) {
    searchParams.set('includeArchived', 'true')
  }

  const data = await api.get('notebooks', { searchParams }).json<unknown>()
  return notebookListSchema.parse(data)
}

export async function getNotebook(id: string): Promise<Notebook> {
  const data = await api.get(`notebooks/${id}`).json<unknown>()
  return notebookSchema.parse(data)
}

export async function createNotebook(payload: NotebookPayload): Promise<Notebook> {
  const data = await api.post('notebooks', { json: payload }).json<unknown>()
  return notebookSchema.parse(data)
}

export async function updateNotebook(id: string, payload: NotebookPayload): Promise<Notebook> {
  const data = await api.put(`notebooks/${id}`, { json: payload }).json<unknown>()
  return notebookSchema.parse(data)
}

export async function setNotebookArchived(id: string, isArchived: boolean): Promise<Notebook> {
  const data = await api.post(`notebooks/${id}/archive`, { json: { isArchived } }).json<unknown>()
  return notebookSchema.parse(data)
}

export async function deleteNotebook(id: string): Promise<void> {
  await api.delete(`notebooks/${id}`)
}
