import { describe, expect, it } from 'vitest'
import type { TaskSummary } from '@/api/schemas'
import { buildColumns } from './board-columns'

let counter = 0
function makeTask(partial: Partial<TaskSummary>): TaskSummary {
  counter += 1
  return {
    promptId: `prompt-${counter}`,
    workingDirectoryId: `wd-${counter}`,
    workingDirectoryName: 'repo',
    title: `Tarefa ${counter}`,
    promptStatus: 'Draft',
    workflowStatus: 'Active',
    currentPhaseId: `phase-${counter}`,
    currentPhaseName: 'Planejamento',
    currentPhaseColor: '#2563eb',
    currentActor: 'ClaudeCode',
    enteredCurrentPhaseAtUtc: '2026-06-01T12:00:00Z',
    updatedAtUtc: '2026-06-01T12:00:00Z',
    hasChildPrompts: false,
    hasLinkedPlan: false,
    rowVersion: '0',
    ...partial,
  }
}

describe('buildColumns', () => {
  it('groups tasks into "Sem fluxo", the template phases and "Concluídas"', () => {
    const tasks = [
      makeTask({ currentPhaseName: 'Planejamento' }),
      makeTask({ currentPhaseName: 'Revisão do plano' }),
      makeTask({
        workflowStatus: null,
        currentPhaseId: null,
        currentPhaseName: null,
        currentActor: null,
        enteredCurrentPhaseAtUtc: null,
        rowVersion: null,
      }),
      makeTask({ workflowStatus: 'Done', currentPhaseName: 'Commit/Merge' }),
    ]

    const columns = buildColumns(tasks, ['Planejamento', 'Revisão do plano', 'Implementação'])
    const titles = columns.map((column) => column.title)

    expect(titles[0]).toBe('Sem fluxo')
    expect(titles).toContain('Implementação')
    expect(titles[titles.length - 1]).toBe('Concluídas')
    expect(columns.find((column) => column.title === 'Planejamento')?.tasks).toHaveLength(1)
    expect(columns.find((column) => column.title === 'Implementação')?.tasks).toHaveLength(0)
    expect(columns.find((column) => column.title === 'Concluídas')?.tasks).toHaveLength(1)
  })

  it('places active tasks with a non-template phase under "Outras fases"', () => {
    const columns = buildColumns([makeTask({ currentPhaseName: 'Fase custom' })], ['Planejamento'])
    expect(columns.find((column) => column.title === 'Fase custom')?.tasks).toHaveLength(1)
  })

  it('omits optional columns when there are no matching tasks', () => {
    const columns = buildColumns([makeTask({ currentPhaseName: 'Planejamento' })], ['Planejamento'])
    expect(columns.map((column) => column.title)).not.toContain('Sem fluxo')
    expect(columns.map((column) => column.title)).not.toContain('Concluídas')
  })
})
