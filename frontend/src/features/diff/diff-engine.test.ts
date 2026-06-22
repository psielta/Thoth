import { describe, expect, it } from 'vitest'
import { computeLineDiff } from './diff-engine'

describe('computeLineDiff', () => {
  it('identical strings → hasChanges=false, all unchanged', () => {
    const result = computeLineDiff('line1\nline2\n', 'line1\nline2\n')
    expect(result.hasChanges).toBe(false)
    expect(result.stats).toEqual({ added: 0, removed: 0 })
    expect(result.unified.every((r) => r.type === 'unchanged')).toBe(true)
    expect(result.unified).toHaveLength(2)
    expect(result.changeHunks.unified).toEqual([])
    expect(result.changeHunks.split).toEqual([])
  })

  it('both empty → no changes, no rows', () => {
    const result = computeLineDiff('', '')
    expect(result.hasChanges).toBe(false)
    expect(result.unified).toHaveLength(0)
    expect(result.split).toHaveLength(0)
  })

  it('pure addition', () => {
    const result = computeLineDiff('line1\n', 'line1\nline2\n')
    expect(result.hasChanges).toBe(true)
    expect(result.stats.added).toBe(1)
    expect(result.stats.removed).toBe(0)
    const added = result.unified.filter((r) => r.type === 'added')
    expect(added).toHaveLength(1)
    expect(added[0].segments[0].value).toBe('line2')
    expect(added[0].newLine).toBe(2)
    expect(added[0].oldLine).toBeNull()
    expect(result.changeHunks.unified).toEqual([1])
    expect(result.changeHunks.split).toEqual([1])
  })

  it('pure removal', () => {
    const result = computeLineDiff('line1\nline2\n', 'line1\n')
    expect(result.hasChanges).toBe(true)
    expect(result.stats.removed).toBe(1)
    expect(result.stats.added).toBe(0)
    const removed = result.unified.filter((r) => r.type === 'removed')
    expect(removed).toHaveLength(1)
    expect(removed[0].segments[0].value).toBe('line2')
    expect(removed[0].oldLine).toBe(2)
    expect(removed[0].newLine).toBeNull()
  })

  it('modified line → word-level segments with emphasis only on changed words', () => {
    const result = computeLineDiff('Hello world\n', 'Hello there\n')
    expect(result.hasChanges).toBe(true)

    const removed = result.unified.filter((r) => r.type === 'removed')
    const added = result.unified.filter((r) => r.type === 'added')
    expect(removed).toHaveLength(1)
    expect(added).toHaveLength(1)

    // "Hello " is unchanged — no emphasis
    const helloSeg = removed[0].segments.find((s) => s.value === 'Hello ')
    expect(helloSeg).toBeDefined()
    expect(helloSeg!.emphasis).toBe(false)

    // "world" was removed — emphasis on removed side
    const worldSeg = removed[0].segments.find((s) => s.value === 'world')
    expect(worldSeg).toBeDefined()
    expect(worldSeg!.emphasis).toBe(true)

    // "there" was added — emphasis on added side
    const thereSeg = added[0].segments.find((s) => s.value === 'there')
    expect(thereSeg).toBeDefined()
    expect(thereSeg!.emphasis).toBe(true)
  })

  it('old empty → all lines are added', () => {
    const result = computeLineDiff('', 'new line\n')
    expect(result.hasChanges).toBe(true)
    expect(result.stats.added).toBe(1)
    expect(result.stats.removed).toBe(0)
    expect(result.unified[0].type).toBe('added')
  })

  it('new empty → all lines are removed', () => {
    const result = computeLineDiff('old line\n', '')
    expect(result.hasChanges).toBe(true)
    expect(result.stats.removed).toBe(1)
    expect(result.stats.added).toBe(0)
    expect(result.unified[0].type).toBe('removed')
  })

  it('correct line numbering for old and new', () => {
    const result = computeLineDiff('a\nb\nc\n', 'a\nX\nc\n')

    const unchanged = result.unified.filter((r) => r.type === 'unchanged')
    expect(unchanged[0].oldLine).toBe(1)
    expect(unchanged[0].newLine).toBe(1)
    expect(unchanged[1].oldLine).toBe(3)
    expect(unchanged[1].newLine).toBe(3)

    const removed = result.unified.find((r) => r.type === 'removed')!
    expect(removed.oldLine).toBe(2)
    expect(removed.newLine).toBeNull()

    const added = result.unified.find((r) => r.type === 'added')!
    expect(added.newLine).toBe(2)
    expect(added.oldLine).toBeNull()
  })

  it('content without trailing newline', () => {
    const result = computeLineDiff('abc', 'abc')
    expect(result.hasChanges).toBe(false)
    expect(result.unified).toHaveLength(1)
    expect(result.unified[0].type).toBe('unchanged')
  })

  it('CRLF normalization: same content with \\r\\n vs \\n → no changes', () => {
    const crlf = 'line1\r\nline2\r\n'
    const lf = 'line1\nline2\n'
    const result = computeLineDiff(crlf, lf)
    expect(result.hasChanges).toBe(false)
  })

  it('multiple lines added vs removed align correctly in split view', () => {
    const result = computeLineDiff('a\nb\n', 'x\ny\nz\n')
    expect(result.stats.removed).toBe(2)
    expect(result.stats.added).toBe(3)

    // split rows: 2 paired (a↔x, b↔y) + 1 extra added (z)
    expect(result.split).toHaveLength(3)
    expect(result.split[0].left?.type).toBe('removed')
    expect(result.split[0].right?.type).toBe('added')
    expect(result.split[2].left).toBeNull()
    expect(result.split[2].right?.type).toBe('added')
    expect(result.changeHunks.unified).toEqual([0])
    expect(result.changeHunks.split).toEqual([0])
  })

  it('modified line → single hunk in unified and split', () => {
    const result = computeLineDiff('a\nb\nc\n', 'a\nX\nc\n')
    expect(result.changeHunks.unified).toEqual([1])
    expect(result.changeHunks.split).toEqual([1])
  })

  it('two separated change blocks → two hunks', () => {
    const result = computeLineDiff('a\nb\nc\nd\n', 'a\nB\nc\nD\n')
    // modified lines produce removed+added pairs; second hunk starts at row 4
    expect(result.changeHunks.unified).toEqual([1, 4])
    expect(result.changeHunks.split).toEqual([1, 3])
  })
})
