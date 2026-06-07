import { api } from './client'
import { type FutureTaskFilters } from './query-keys'
import {
  futureTaskListSchema,
  futureTaskSchema,
  type FutureTask,
  type FutureTaskStatus,
  type FutureTaskType,
} from './schemas'

export type FutureTaskPayload = {
  workingDirectoryId: string
  title: string
  description: string
  type: FutureTaskType
  labels: string[]
  issueGithubId?: string | null
}

export type UpdateFutureTaskPayload = {
  title: string
  description: string
  type: FutureTaskType
  labels: string[]
  issueGithubId?: string | null
  rowVersion: string
}

export async function listFutureTasks(filters: FutureTaskFilters): Promise<FutureTask[]> {
  const searchParams = new URLSearchParams()
  if (filters.workingDirectoryId) {
    searchParams.set('workingDirectoryId', filters.workingDirectoryId)
  }
  if (filters.status) {
    searchParams.set('status', filters.status)
  }
  if (filters.type) {
    searchParams.set('type', filters.type)
  }
  if (filters.label) {
    searchParams.set('label', filters.label)
  }
  if (filters.includeArchived) {
    searchParams.set('includeArchived', 'true')
  }
  if (filters.q) {
    searchParams.set('q', filters.q)
  }

  const data = await api.get('future-tasks', { searchParams }).json<unknown>()
  return futureTaskListSchema.parse(data)
}

export async function getFutureTask(id: string): Promise<FutureTask> {
  const data = await api.get(`future-tasks/${id}`).json<unknown>()
  return futureTaskSchema.parse(data)
}

export async function createFutureTask(payload: FutureTaskPayload): Promise<FutureTask> {
  const data = await api.post('future-tasks', { json: payload }).json<unknown>()
  return futureTaskSchema.parse(data)
}

export async function updateFutureTask(id: string, payload: UpdateFutureTaskPayload): Promise<FutureTask> {
  const data = await api.put(`future-tasks/${id}`, { json: payload }).json<unknown>()
  return futureTaskSchema.parse(data)
}

export async function updateFutureTaskStatus(
  id: string,
  status: FutureTaskStatus,
  rowVersion: string,
): Promise<FutureTask> {
  const data = await api.patch(`future-tasks/${id}/status`, { json: { status, rowVersion } }).json<unknown>()
  return futureTaskSchema.parse(data)
}

export async function deleteFutureTask(id: string) {
  await api.delete(`future-tasks/${id}`)
}
