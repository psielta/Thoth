import { describe, expect, it } from 'vitest'
import { futureTaskStatusSchema, futureTaskTypeSchema, type FutureTask } from '@/api/schemas'
import { buildCreateIssuePrompt } from './build-create-issue-prompt'
import { buildSeededPromptContent } from './seed-prompt-content'
import { STATUS_OPTIONS, TYPE_OPTIONS, futureTaskFormSchema } from './constants'

function makeTask(overrides: Partial<FutureTask> = {}): FutureTask {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    workingDirectoryId: '00000000-0000-0000-0000-000000000002',
    title: 'Add dark mode',
    description: 'Support a dark theme',
    status: 'Open',
    type: 'Feature',
    labels: ['frontend', 'ai'],
    issueGithubId: null,
    rowVersion: '1',
    linkedPromptCount: 0,
    createdAtUtc: '2026-06-07T00:00:00Z',
    updatedAtUtc: '2026-06-07T00:00:00Z',
    ...overrides,
  }
}

describe('future task constants', () => {
  it('covers all enum values exposed by the API schemas', () => {
    expect(STATUS_OPTIONS.map((option) => option.value).sort()).toEqual([...futureTaskStatusSchema.options].sort())
    expect(TYPE_OPTIONS.map((option) => option.value).sort()).toEqual([...futureTaskTypeSchema.options].sort())
  })

  it('validates future task form values', () => {
    expect(
      futureTaskFormSchema.safeParse({ title: 'ab', description: '', type: 'Task', labels: [], issueGithubId: '' })
        .success,
    ).toBe(false)

    expect(
      futureTaskFormSchema.safeParse({
        title: 'Valid title',
        description: '',
        type: 'Bug',
        labels: ['backend'],
        issueGithubId: '',
      }).success,
    ).toBe(true)
  })
})

describe('buildSeededPromptContent', () => {
  it('embeds the task content when there is no github issue id', () => {
    const content = buildSeededPromptContent(makeTask({ issueGithubId: null }))
    expect(content).toContain('Please work on issue below in this repo. Then, open a PR')
    expect(content).toContain('# Add dark mode')
    expect(content).toContain('Support a dark theme')
    expect(content).not.toContain('GitHub issue #')
  })

  it('references the github issue when an id is set', () => {
    const content = buildSeededPromptContent(makeTask({ issueGithubId: '42' }))
    expect(content).toContain('Please work on GitHub issue #42 in this repo. Then open a PR')
    expect(content).toContain('# Add dark mode')
  })

  it('omits the description block when empty', () => {
    const content = buildSeededPromptContent(makeTask({ description: '   ' }))
    expect(content.trim().endsWith('# Add dark mode')).toBe(true)
  })
})

describe('buildCreateIssuePrompt', () => {
  it('includes best-practice guidance, title, labels and description', () => {
    const prompt = buildCreateIssuePrompt(makeTask({ labels: ['backend', 'priority:high'] }))
    expect(prompt).toContain('following best practices')
    expect(prompt).toContain('Title: Add dark mode')
    expect(prompt).toContain('backend, priority:high')
    expect(prompt).toContain('Support a dark theme')
  })

  it('falls back when no labels are set', () => {
    const prompt = buildCreateIssuePrompt(makeTask({ labels: [] }))
    expect(prompt).toContain('(choose appropriate labels)')
  })
})
