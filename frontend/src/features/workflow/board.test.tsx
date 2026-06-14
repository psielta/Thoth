import { describe, expect, it } from 'vitest'
import type { TaskSummary } from '@/api/schemas'
import { buildColumns } from './board-columns'
import { computeReorderedIds, shouldShowDropPlaceholder } from './drop-placeholder-state'

const templatePhases = [
  { name: 'Planejamento', orderIndex: 0 },
  { name: 'Revisao do plano', orderIndex: 1 },
  { name: 'Implementacao', orderIndex: 2 },
]

let counter = 0
function makeTask(partial: Partial<TaskSummary>): TaskSummary {
  counter += 1
  return {
    promptId: `prompt-${counter}`,
    workingDirectoryId: `wd-${counter}`,
    workingDirectoryName: 'repo',
    taskNumber: null,
    title: `Tarefa ${counter}`,
    promptStatus: 'Draft',
    workflowStatus: 'Active',
    currentPhaseId: `phase-${counter}`,
    currentPhaseName: 'Planejamento',
    currentPhaseColor: '#2563eb',
    currentActor: 'ClaudeCode',
    enteredCurrentPhaseAtUtc: '2026-06-01T12:00:00Z',
    currentPhaseIteration: 1,
    reviewVerdictSourcePhaseName: null,
    updatedAtUtc: '2026-06-01T12:00:00Z',
    hasChildPrompts: false,
    hasLinkedPlan: false,
    linkedDocumentId: null,
    pullRequestReference: null,
    promptRowVersion: '0',
    phases: [
      { id: 'phase-planning', name: 'Planejamento', defaultActor: 'ClaudeCode', orderIndex: 0, color: '#2563eb', role: 'Planning' },
      { id: 'phase-review', name: 'Revisao do plano', defaultActor: 'Codex', orderIndex: 1, color: '#7c3aed', role: 'PlanReview' },
      { id: 'phase-implementation', name: 'Implementacao', defaultActor: 'Codex', orderIndex: 2, color: '#0d9488', role: 'Implementation' },
    ],
    rowVersion: '0',
    ...partial,
  }
}

describe('buildColumns', () => {
  it('groups tasks into "Sem fluxo", the template phases and "Concluídas"', () => {
    const tasks = [
      makeTask({ currentPhaseName: 'Planejamento' }),
      makeTask({ currentPhaseName: 'Revisao do plano' }),
      makeTask({
        workflowStatus: null,
        currentPhaseId: null,
        currentPhaseName: null,
        currentActor: null,
        enteredCurrentPhaseAtUtc: null,
        phases: [],
        rowVersion: null,
      }),
      makeTask({ workflowStatus: 'Done', currentPhaseName: 'Commit/Merge' }),
    ]

    const columns = buildColumns(tasks, templatePhases)
    const titles = columns.map((column) => column.title)

    expect(titles[0]).toBe('Sem fluxo')
    expect(titles).toContain('Implementacao')
    expect(titles[titles.length - 1]).toBe('Concluídas')
    expect(columns.find((column) => column.title === 'Planejamento')?.tasks).toHaveLength(1)
    expect(columns.find((column) => column.title === 'Implementacao')?.tasks).toHaveLength(0)
    expect(columns.find((column) => column.title === 'Concluídas')?.tasks).toHaveLength(1)
    expect(columns.find((column) => column.title === 'Planejamento')?.droppable).toBe(true)
    expect(columns.find((column) => column.title === 'Sem fluxo')?.droppable).toBe(false)
  })

  it('places active tasks with a non-template phase under its own column', () => {
    const columns = buildColumns([makeTask({ currentPhaseName: 'Fase custom' })], templatePhases)
    expect(columns.find((column) => column.title === 'Fase custom')?.tasks).toHaveLength(1)
  })

  it('omits the optional no-workflow column when there are no matching tasks', () => {
    const columns = buildColumns([makeTask({ currentPhaseName: 'Planejamento' })], templatePhases)
    expect(columns.map((column) => column.title)).not.toContain('Sem fluxo')
    expect(columns.map((column) => column.title)).toContain('Concluídas')
  })
})

describe('shouldShowDropPlaceholder', () => {
  it('shows only for the active target column and index while dragging', () => {
    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: true,
        draggedPromptId: 'prompt-1',
        dropTarget: { columnId: 'phase-1', index: 2 },
        placeholderIndex: 2,
      }),
    ).toBe(true)

    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: true,
        draggedPromptId: 'prompt-1',
        dropTarget: { columnId: 'phase-2', index: 2 },
        placeholderIndex: 2,
      }),
    ).toBe(false)

    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: true,
        draggedPromptId: 'prompt-1',
        dropTarget: { columnId: 'phase-1', index: 1 },
        placeholderIndex: 2,
      }),
    ).toBe(false)

    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: false,
        draggedPromptId: 'prompt-1',
        dropTarget: { columnId: 'phase-1', index: 2 },
        placeholderIndex: 2,
      }),
    ).toBe(false)

    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: true,
        draggedPromptId: null,
        dropTarget: { columnId: 'phase-1', index: 2 },
        placeholderIndex: 2,
      }),
    ).toBe(false)
  })

  it('hides while a move mutation is pending', () => {
    expect(
      shouldShowDropPlaceholder({
        columnId: 'phase-1',
        acceptsDrop: true,
        draggedPromptId: 'prompt-1',
        dropTarget: { columnId: 'phase-1', index: 0 },
        placeholderIndex: 0,
        isMoving: true,
      }),
    ).toBe(false)
  })
})

describe('computeReorderedIds', () => {
  it('moves a card to an earlier slot', () => {
    expect(computeReorderedIds(['a', 'b', 'c'], 'c', 0)).toEqual(['c', 'a', 'b'])
  })

  it('moves a card to a later slot while compensating for removal', () => {
    expect(computeReorderedIds(['a', 'b', 'c', 'd'], 'b', 3)).toEqual(['a', 'c', 'b', 'd'])
  })

  it('keeps the order when dropping immediately after itself', () => {
    expect(computeReorderedIds(['a', 'b', 'c'], 'b', 2)).toEqual(['a', 'b', 'c'])
  })

  it('returns the original ids when the dragged card is not in the column', () => {
    expect(computeReorderedIds(['a', 'b'], 'x', 1)).toEqual(['a', 'b'])
  })
})
