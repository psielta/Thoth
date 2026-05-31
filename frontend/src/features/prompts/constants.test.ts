import { describe, expect, it } from 'vitest'
import { promptKindSchema, promptStatusSchema, targetAgentSchema } from '@/api/schemas'
import { AGENT_OPTIONS, KIND_OPTIONS, STATUS_OPTIONS, promptFormSchema } from './constants'

describe('prompt constants', () => {
  it('covers all enum values exposed by the API schemas', () => {
    expect(AGENT_OPTIONS.map((option) => option.value).sort()).toEqual([...targetAgentSchema.options].sort())
    expect(KIND_OPTIONS.map((option) => option.value).sort()).toEqual([...promptKindSchema.options].sort())
    expect(STATUS_OPTIONS.map((option) => option.value).sort()).toEqual([...promptStatusSchema.options].sort())
  })

  it('validates prompt form values', () => {
    expect(
      promptFormSchema.safeParse({
        title: 'ab',
        targetAgent: 'Codex',
        kind: 'General',
        status: 'Draft',
        content: 'ok',
      }).success,
    ).toBe(false)

    expect(
      promptFormSchema.safeParse({
        title: 'Valid title',
        targetAgent: 'Codex',
        kind: 'Planning',
        status: 'Draft',
        content: 'Valid content',
      }).success,
    ).toBe(true)
  })
})
