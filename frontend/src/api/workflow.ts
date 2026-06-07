import { api } from './client'
import { type WorkflowBoardFilters } from './query-keys'
import {
  taskSummaryListSchema,
  workflowSchema,
  workflowTemplateSchema,
  type TaskSummary,
  type Workflow,
  type WorkflowActor,
  type WorkflowTemplate,
} from './schemas'

export type WorkflowPhaseInput = {
  id: string | null
  name: string
  defaultActor: WorkflowActor
  orderIndex: number
  color: string
}

export async function getBoard(filters: WorkflowBoardFilters = {}): Promise<TaskSummary[]> {
  const searchParams = new URLSearchParams()
  if (filters.workflowStatus) {
    searchParams.set('workflowStatus', filters.workflowStatus)
  }
  if (filters.promptStatus) {
    searchParams.set('promptStatus', filters.promptStatus)
  }
  if (filters.workingDirectoryId) {
    searchParams.set('workingDirectoryId', filters.workingDirectoryId)
  }
  if (filters.q) {
    searchParams.set('q', filters.q)
  }

  const data = await api.get('workflow/board', { searchParams }).json<unknown>()
  return taskSummaryListSchema.parse(data)
}

export async function getWorkflow(promptId: string): Promise<Workflow | null> {
  const data = await api.get(`prompts/${promptId}/workflow`).json<unknown>()
  if (data === null || data === undefined) {
    return null
  }

  return workflowSchema.parse(data)
}

export async function startWorkflow(promptId: string, initialPhaseOrderIndex?: number): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow`, { json: { initialPhaseOrderIndex: initialPhaseOrderIndex ?? null } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function advancePhase(promptId: string, rowVersion: string, note?: string): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/advance`, { json: { rowVersion, note: note ?? null } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function setPhase(
  promptId: string,
  phaseId: string,
  rowVersion: string,
  actor?: WorkflowActor,
  note?: string,
): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/phase`, { json: { phaseId, actor: actor ?? null, note: note ?? null, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function changeActor(
  promptId: string,
  actor: WorkflowActor,
  rowVersion: string,
  note?: string,
): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/actor`, { json: { actor, note: note ?? null, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function addWorkflowNote(promptId: string, note: string): Promise<Workflow> {
  const data = await api.post(`prompts/${promptId}/workflow/notes`, { json: { note } }).json<unknown>()
  return workflowSchema.parse(data)
}

export async function addReviewVerdict(promptId: string, verdict: string, rowVersion: string): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/review-verdict`, { json: { verdict, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function completeWorkflow(promptId: string, rowVersion: string, note?: string): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/complete`, { json: { note: note ?? null, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function reopenWorkflow(promptId: string, rowVersion: string, phaseId?: string): Promise<Workflow> {
  const data = await api
    .post(`prompts/${promptId}/workflow/reopen`, { json: { phaseId: phaseId ?? null, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function updateTaskPhases(
  promptId: string,
  phases: WorkflowPhaseInput[],
  rowVersion: string,
): Promise<Workflow> {
  const data = await api
    .put(`prompts/${promptId}/workflow/phases`, { json: { phases, rowVersion } })
    .json<unknown>()
  return workflowSchema.parse(data)
}

export async function getWorkflowTemplate(): Promise<WorkflowTemplate> {
  const data = await api.get('workflow/template').json<unknown>()
  return workflowTemplateSchema.parse(data)
}

export async function updateWorkflowTemplate(phases: WorkflowPhaseInput[]): Promise<WorkflowTemplate> {
  const data = await api.put('workflow/template', { json: { phases } }).json<unknown>()
  return workflowTemplateSchema.parse(data)
}
