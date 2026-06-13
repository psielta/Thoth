import { describe, expect, it } from 'vitest'
import { terminalGroupSchema } from './schemas'

const validGroup = {
  promptId: '11111111-1111-4111-8111-111111111111',
  promptTitle: 'Refatorar auth',
  workingDirectoryId: '22222222-2222-4222-8222-222222222222',
  workingDirectoryName: 'repo',
  isArchived: false,
  terminals: [
    {
      id: '33333333-3333-4333-8333-333333333333',
      promptId: '11111111-1111-4111-8111-111111111111',
      shell: 'pwsh.exe',
      cwd: 'D:/repo',
      createdAtUtc: '2026-06-13T12:00:00Z',
    },
  ],
}

describe('terminalGroupSchema', () => {
  it('parses a valid grouped payload', () => {
    const parsed = terminalGroupSchema.parse(validGroup)
    expect(parsed.promptTitle).toBe('Refatorar auth')
    expect(parsed.terminals).toHaveLength(1)
    expect(parsed.terminals[0].cwd).toBe('D:/repo')
  })

  it('rejects a non-uuid prompt id', () => {
    expect(() => terminalGroupSchema.parse({ ...validGroup, promptId: 'not-a-uuid' })).toThrow()
  })

  it('rejects a payload missing terminals', () => {
    const incomplete = { ...validGroup } as Record<string, unknown>
    delete incomplete.terminals
    expect(() => terminalGroupSchema.parse(incomplete)).toThrow()
  })
})
