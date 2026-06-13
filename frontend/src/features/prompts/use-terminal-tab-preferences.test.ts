import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import {
  terminalTabPreferencesStorageKey,
  type TerminalTabPreferencesMap,
} from './terminal-tab-preferences'
import { useTerminalTabPreferences } from './use-terminal-tab-preferences'

const promptId = '11111111-1111-4111-8111-111111111111'
const sessionId = '22222222-2222-4222-8222-222222222222'

describe('useTerminalTabPreferences', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('restores saved name and color after refresh when sessions load later', () => {
    const stored: TerminalTabPreferencesMap = {
      [sessionId]: { name: 'Codex', color: '#16c60c' },
    }
    localStorage.setItem(terminalTabPreferencesStorageKey(promptId), JSON.stringify(stored))

    const { result, rerender } = renderHook(
      ({ sessionIds }) => useTerminalTabPreferences(promptId, sessionIds),
      { initialProps: { sessionIds: [] as string[] } },
    )

    expect(result.current.preferences).toEqual(stored)

    rerender({ sessionIds: [sessionId] })

    expect(result.current.preferences).toEqual(stored)
  })

  it('persists updates to localStorage by session id', () => {
    const { result } = renderHook(() => useTerminalTabPreferences(promptId, [sessionId]))

    act(() => {
      result.current.setSessionPreference(sessionId, { name: 'Grok', color: '#ff8c00' })
    })

    expect(result.current.preferences[sessionId]).toEqual({ name: 'Grok', color: '#ff8c00' })
    expect(JSON.parse(localStorage.getItem(terminalTabPreferencesStorageKey(promptId)) ?? '{}')).toEqual({
      [sessionId]: { name: 'Grok', color: '#ff8c00' },
    })
  })
})