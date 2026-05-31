import { diffLines, diffWordsWithSpace } from 'diff'

export type DiffSegment = { value: string; emphasis: boolean }
export type DiffRowType = 'unchanged' | 'added' | 'removed'

export interface UnifiedRow {
  type: DiffRowType
  oldLine: number | null
  newLine: number | null
  segments: DiffSegment[]
}

export interface SplitCell {
  type: DiffRowType
  line: number
  segments: DiffSegment[]
}

export interface SplitRow {
  left: SplitCell | null
  right: SplitCell | null
}

export interface DiffModel {
  unified: UnifiedRow[]
  split: SplitRow[]
  hasChanges: boolean
  stats: { added: number; removed: number }
}

function normalizeLineEndings(text: string): string {
  return text.replace(/\r\n/g, '\n').replace(/\r/g, '\n')
}

function splitIntoLines(value: string): string[] {
  if (value === '') return []
  const lines = value.split('\n')
  if (lines[lines.length - 1] === '') lines.pop()
  return lines
}

function plain(line: string): DiffSegment[] {
  return [{ value: line, emphasis: false }]
}

export function computeLineDiff(oldContent: string, newContent: string): DiffModel {
  const normalizedOld = normalizeLineEndings(oldContent)
  const normalizedNew = normalizeLineEndings(newContent)

  const parts = diffLines(normalizedOld, normalizedNew)

  const unified: UnifiedRow[] = []
  const split: SplitRow[] = []
  const stats = { added: 0, removed: 0 }
  let oldLine = 1
  let newLine = 1

  let i = 0
  while (i < parts.length) {
    const part = parts[i]

    if (part.removed && i + 1 < parts.length && parts[i + 1].added) {
      const removedLines = splitIntoLines(part.value)
      const addedLines = splitIntoLines(parts[i + 1].value)
      const maxLen = Math.max(removedLines.length, addedLines.length)

      for (let j = 0; j < maxLen; j++) {
        const rl = removedLines[j]
        const al = addedLines[j]

        if (rl !== undefined && al !== undefined) {
          const wordChanges = diffWordsWithSpace(rl, al)
          const leftSegs: DiffSegment[] = wordChanges
            .filter((w) => !w.added)
            .map((w) => ({ value: w.value, emphasis: !!w.removed }))
          const rightSegs: DiffSegment[] = wordChanges
            .filter((w) => !w.removed)
            .map((w) => ({ value: w.value, emphasis: !!w.added }))

          unified.push({ type: 'removed', oldLine, newLine: null, segments: leftSegs })
          unified.push({ type: 'added', oldLine: null, newLine, segments: rightSegs })
          split.push({
            left: { type: 'removed', line: oldLine, segments: leftSegs },
            right: { type: 'added', line: newLine, segments: rightSegs },
          })
          oldLine++
          newLine++
          stats.removed++
          stats.added++
        } else if (rl !== undefined) {
          const segs = plain(rl)
          unified.push({ type: 'removed', oldLine, newLine: null, segments: segs })
          split.push({ left: { type: 'removed', line: oldLine, segments: segs }, right: null })
          oldLine++
          stats.removed++
        } else {
          const segs = plain(al!)
          unified.push({ type: 'added', oldLine: null, newLine, segments: segs })
          split.push({ left: null, right: { type: 'added', line: newLine, segments: segs } })
          newLine++
          stats.added++
        }
      }
      i += 2
    } else if (part.removed) {
      for (const line of splitIntoLines(part.value)) {
        const segs = plain(line)
        unified.push({ type: 'removed', oldLine, newLine: null, segments: segs })
        split.push({ left: { type: 'removed', line: oldLine, segments: segs }, right: null })
        oldLine++
        stats.removed++
      }
      i++
    } else if (part.added) {
      for (const line of splitIntoLines(part.value)) {
        const segs = plain(line)
        unified.push({ type: 'added', oldLine: null, newLine, segments: segs })
        split.push({ left: null, right: { type: 'added', line: newLine, segments: segs } })
        newLine++
        stats.added++
      }
      i++
    } else {
      for (const line of splitIntoLines(part.value)) {
        const segs = plain(line)
        unified.push({ type: 'unchanged', oldLine, newLine, segments: segs })
        split.push({
          left: { type: 'unchanged', line: oldLine, segments: segs },
          right: { type: 'unchanged', line: newLine, segments: segs },
        })
        oldLine++
        newLine++
      }
      i++
    }
  }

  return {
    unified,
    split,
    hasChanges: stats.added > 0 || stats.removed > 0,
    stats,
  }
}
