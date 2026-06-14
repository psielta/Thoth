import { describe, expect, it } from 'vitest'
import type { TerminalOutputHistory } from '@/api/schemas'
import { bytesToBase64 } from '@/lib/base64'
import { applyTerminalOutputChunk, applyTerminalOutputHistory } from './terminal-output-buffer'

function encoded(text: string) {
  return bytesToBase64(new TextEncoder().encode(text))
}

function decoded(bytes: Uint8Array) {
  return new TextDecoder().decode(bytes)
}

describe('applyTerminalOutputChunk', () => {
  it('ignores duplicate chunks', () => {
    const result = applyTerminalOutputChunk(
      { endOffset: 5 },
      { startOffset: 0, dataBase64: encoded('hello') },
    )

    expect(result).toEqual({ type: 'ignored', endOffset: 5 })
  })

  it('writes only the new bytes from an overlapping chunk', () => {
    const result = applyTerminalOutputChunk(
      { endOffset: 3 },
      { startOffset: 0, dataBase64: encoded('hello') },
    )

    expect(result.type).toBe('write')
    if (result.type === 'write') {
      expect(decoded(result.bytes)).toBe('lo')
      expect(result.endOffset).toBe(5)
    }
  })

  it('writes contiguous chunks', () => {
    const result = applyTerminalOutputChunk(
      { endOffset: 5 },
      { startOffset: 5, dataBase64: encoded(' world') },
    )

    expect(result.type).toBe('write')
    if (result.type === 'write') {
      expect(decoded(result.bytes)).toBe(' world')
      expect(result.endOffset).toBe(11)
    }
  })

  it('reports gaps without advancing the expected offset', () => {
    const result = applyTerminalOutputChunk(
      { endOffset: 5 },
      { startOffset: 8, dataBase64: encoded('tail') },
    )

    expect(result).toEqual({
      type: 'gap',
      expectedOffset: 5,
      startOffset: 8,
      endOffset: 12,
    })
  })
})

describe('applyTerminalOutputHistory', () => {
  it('fills a gap when history still covers the expected offset', () => {
    const history: TerminalOutputHistory = {
      sessionId: '11111111-1111-4111-8111-111111111111',
      startOffset: 0,
      endOffset: 10,
      dataBase64: encoded('abcdefghij'),
      isTruncated: false,
    }

    const result = applyTerminalOutputHistory({ endOffset: 5 }, history)

    expect(result.type).toBe('write')
    if (result.type === 'write') {
      expect(decoded(result.bytes)).toBe('fghij')
      expect(result.endOffset).toBe(10)
      expect(result.isTruncated).toBe(false)
    }
  })

  it('requests a reset when history was truncated before the expected offset', () => {
    const history: TerminalOutputHistory = {
      sessionId: '11111111-1111-4111-8111-111111111111',
      startOffset: 8,
      endOffset: 12,
      dataBase64: encoded('tail'),
      isTruncated: true,
    }

    const result = applyTerminalOutputHistory({ endOffset: 5 }, history)

    expect(result.type).toBe('reset')
    if (result.type === 'reset') {
      expect(decoded(result.bytes)).toBe('tail')
      expect(result.startOffset).toBe(8)
      expect(result.endOffset).toBe(12)
      expect(result.isTruncated).toBe(true)
    }
  })
})
