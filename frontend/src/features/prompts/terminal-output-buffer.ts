import type { TerminalOutputHistory } from '@/api/schemas'
import { base64ToBytes } from '@/lib/base64'

type TerminalOutputState = {
  endOffset: number
}

export type TerminalOutputChunk = {
  startOffset: number
  dataBase64: string
}

export type TerminalOutputApplyResult =
  | {
      type: 'ignored'
      endOffset: number
    }
  | {
      type: 'write'
      bytes: Uint8Array
      endOffset: number
    }
  | {
      type: 'gap'
      expectedOffset: number
      startOffset: number
      endOffset: number
    }

export type TerminalHistoryApplyResult =
  | {
      type: 'ignored'
      endOffset: number
      isTruncated: boolean
    }
  | {
      type: 'write'
      bytes: Uint8Array
      endOffset: number
      isTruncated: boolean
    }
  | {
      type: 'reset'
      bytes: Uint8Array
      startOffset: number
      endOffset: number
      isTruncated: boolean
    }

export function applyTerminalOutputChunk(
  state: TerminalOutputState,
  chunk: TerminalOutputChunk,
): TerminalOutputApplyResult {
  const bytes = base64ToBytes(chunk.dataBase64)
  const endOffset = chunk.startOffset + bytes.length

  if (endOffset <= state.endOffset) {
    return { type: 'ignored', endOffset: state.endOffset }
  }

  if (chunk.startOffset > state.endOffset) {
    return {
      type: 'gap',
      expectedOffset: state.endOffset,
      startOffset: chunk.startOffset,
      endOffset,
    }
  }

  const overlap = Math.max(state.endOffset - chunk.startOffset, 0)
  return {
    type: 'write',
    bytes: overlap > 0 ? bytes.slice(overlap) : bytes,
    endOffset,
  }
}

export function applyTerminalOutputHistory(
  state: TerminalOutputState,
  history: TerminalOutputHistory,
): TerminalHistoryApplyResult {
  const bytes = base64ToBytes(history.dataBase64)

  if (history.endOffset <= state.endOffset) {
    return {
      type: 'ignored',
      endOffset: state.endOffset,
      isTruncated: history.isTruncated,
    }
  }

  if (history.startOffset > state.endOffset) {
    return {
      type: 'reset',
      bytes,
      startOffset: history.startOffset,
      endOffset: history.endOffset,
      isTruncated: true,
    }
  }

  const overlap = Math.max(state.endOffset - history.startOffset, 0)
  return {
    type: 'write',
    bytes: overlap > 0 ? bytes.slice(overlap) : bytes,
    endOffset: history.endOffset,
    isTruncated: history.isTruncated,
  }
}
