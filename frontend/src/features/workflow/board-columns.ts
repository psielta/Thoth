import type { TaskSummary } from '@/api/schemas'

export type BoardColumn = { title: string; tasks: TaskSummary[] }

/**
 * Groups board tasks into "Sem fluxo", one column per template phase (in order),
 * any non-template phases that still have active tasks, and a trailing "Concluídas".
 */
export function buildColumns(tasks: TaskSummary[], templatePhaseNames: string[]): BoardColumn[] {
  const noWorkflow = tasks.filter((task) => task.workflowStatus === null)
  const done = tasks.filter((task) => task.workflowStatus === 'Done')
  const active = tasks.filter((task) => task.workflowStatus === 'Active')

  const activeByPhase = new Map<string, TaskSummary[]>()
  for (const task of active) {
    const key = task.currentPhaseName ?? 'Outras fases'
    const list = activeByPhase.get(key) ?? []
    list.push(task)
    activeByPhase.set(key, list)
  }

  const columns: BoardColumn[] = []
  if (noWorkflow.length > 0) {
    columns.push({ title: 'Sem fluxo', tasks: noWorkflow })
  }

  for (const name of templatePhaseNames) {
    columns.push({ title: name, tasks: activeByPhase.get(name) ?? [] })
  }

  for (const [name, list] of activeByPhase) {
    if (!templatePhaseNames.includes(name)) {
      columns.push({ title: name, tasks: list })
    }
  }

  if (done.length > 0) {
    columns.push({ title: 'Concluídas', tasks: done })
  }

  return columns
}
