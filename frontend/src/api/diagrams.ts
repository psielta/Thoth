import { api } from './client'
import { type DiagramFilters } from './query-keys'
import {
  diagramSchema,
  diagramSummaryListSchema,
  type Diagram,
  type DiagramSummary,
  type DiagramType,
} from './schemas'

export type CreateDiagramPayload = {
  workingDirectoryId: string
  title: string
  type: DiagramType
  description?: string | null
  content?: string
  metadataJson?: string | null
}

export type UpdateDiagramPayload = {
  title: string
  content: string
  description?: string | null
  metadataJson?: string | null
}

export async function listDiagrams(filters: DiagramFilters): Promise<DiagramSummary[]> {
  const searchParams = new URLSearchParams()
  searchParams.set('workingDirectoryId', filters.workingDirectoryId)
  if (filters.type) {
    searchParams.set('type', filters.type)
  }
  if (filters.q) {
    searchParams.set('q', filters.q)
  }
  if (filters.includeArchived) {
    searchParams.set('includeArchived', 'true')
  }

  const data = await api.get('diagrams', { searchParams }).json<unknown>()
  return diagramSummaryListSchema.parse(data)
}

export async function getDiagram(id: string): Promise<Diagram> {
  const data = await api.get(`diagrams/${id}`).json<unknown>()
  return diagramSchema.parse(data)
}

export async function createDiagram(payload: CreateDiagramPayload): Promise<Diagram> {
  const data = await api.post('diagrams', { json: payload }).json<unknown>()
  return diagramSchema.parse(data)
}

export async function updateDiagram(id: string, payload: UpdateDiagramPayload): Promise<Diagram> {
  const data = await api.put(`diagrams/${id}`, { json: payload }).json<unknown>()
  return diagramSchema.parse(data)
}

export async function setDiagramArchived(id: string, isArchived: boolean): Promise<Diagram> {
  const data = await api.post(`diagrams/${id}/archive`, { json: { isArchived } }).json<unknown>()
  return diagramSchema.parse(data)
}

export async function deleteDiagram(id: string): Promise<void> {
  await api.delete(`diagrams/${id}`)
}
