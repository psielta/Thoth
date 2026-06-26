type FenceState = {
  marker: '`' | '~'
  length: number
}

export function normalizeMarkdownTableBlocks(markdown: string): string {
  if (!markdown.includes('|')) {
    return markdown
  }

  const lines = markdown.replace(/\r\n?/g, '\n').split('\n')
  const output: string[] = []
  let fence: FenceState | null = null

  for (let index = 0; index < lines.length;) {
    const line = lines[index]
    const fenceMatch = matchFence(line)

    if (fenceMatch) {
      fence =
        fence && fence.marker === fenceMatch.marker && fenceMatch.length >= fence.length
          ? null
          : fence ?? fenceMatch
      output.push(line)
      index += 1
      continue
    }

    if (fence || !isCandidateTableRow(line)) {
      output.push(line)
      index += 1
      continue
    }

    const segment: string[] = []
    const tableRows: string[] = []
    let cursor = index

    while (cursor < lines.length) {
      const current = lines[cursor]

      if (isCandidateTableRow(current)) {
        segment.push(current)
        tableRows.push(current)
        cursor += 1
        continue
      }

      if (current.trim() === '' && nextNonBlankLineIsTableRow(lines, cursor + 1)) {
        segment.push(current)
        cursor += 1
        continue
      }

      break
    }

    output.push(...(isValidGfmTableGroup(tableRows) ? tableRows : segment))
    index = cursor
  }

  return output.join('\n')
}

function matchFence(line: string): FenceState | null {
  const match = line.match(/^ {0,3}(`{3,}|~{3,})/)
  if (!match) {
    return null
  }

  const sequence = match[1]
  return {
    marker: sequence[0] as '`' | '~',
    length: sequence.length,
  }
}

function nextNonBlankLineIsTableRow(lines: string[], start: number): boolean {
  for (let index = start; index < lines.length; index += 1) {
    if (lines[index].trim() === '') {
      continue
    }

    return isCandidateTableRow(lines[index])
  }

  return false
}

function isCandidateTableRow(line: string): boolean {
  const trimmed = line.trim()
  return trimmed.startsWith('|') && trimmed.endsWith('|') && splitTableRow(trimmed).length >= 2
}

function isValidGfmTableGroup(rows: string[]): boolean {
  if (rows.length < 2) {
    return false
  }

  const headerCells = splitTableRow(rows[0])
  const delimiterCells = splitTableRow(rows[1])
  return (
    headerCells.length >= 2 &&
    headerCells.length === delimiterCells.length &&
    delimiterCells.every((cell) => /^:?-{3,}:?$/.test(cell.trim()))
  )
}

function splitTableRow(line: string): string[] {
  return line
    .trim()
    .replace(/^\|/, '')
    .replace(/\|$/, '')
    .split('|')
    .map((cell) => cell.trim())
}
