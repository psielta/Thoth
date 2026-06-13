import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useTerminalSwitcher } from './use-terminal-switcher'

describe('useTerminalSwitcher', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('opens switcher on Ctrl+` and selects on Enter', () => {
    const onSelectSession = vi.fn()
    const { result } = renderHook(() =>
      useTerminalSwitcher({
        enabled: true,
        sessionIds: ['a', 'b', 'c'],
        activeSessionId: 'a',
        onSelectSession,
      }),
    )

    act(() => {
      result.current.handleKeyboardEvent(
        new KeyboardEvent('keydown', { key: '`', code: 'Backquote', ctrlKey: true, bubbles: true }),
      )
    })

    expect(result.current.switcherOpen).toBe(true)
    expect(result.current.highlightedSessionId).toBe('b')

    act(() => {
      result.current.handleKeyboardEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))
    })

    expect(onSelectSession).toHaveBeenCalledWith('b')
    expect(result.current.switcherOpen).toBe(false)
  })

  it('switches directly on Ctrl+Alt+ArrowRight', () => {
    const onSelectSession = vi.fn()
    const { result } = renderHook(() =>
      useTerminalSwitcher({
        enabled: true,
        sessionIds: ['a', 'b', 'c'],
        activeSessionId: 'a',
        onSelectSession,
      }),
    )

    act(() => {
      result.current.handleKeyboardEvent(
        new KeyboardEvent('keydown', {
          key: 'ArrowRight',
          ctrlKey: true,
          altKey: true,
          bubbles: true,
        }),
      )
    })

    expect(onSelectSession).toHaveBeenCalledWith('b')
    expect(result.current.switcherOpen).toBe(false)
  })

  it('cycles with Tab while switcher is open', () => {
    const onSelectSession = vi.fn()
    const { result } = renderHook(() =>
      useTerminalSwitcher({
        enabled: true,
        sessionIds: ['a', 'b', 'c'],
        activeSessionId: 'a',
        onSelectSession,
      }),
    )

    act(() => {
      result.current.handleKeyboardEvent(
        new KeyboardEvent('keydown', { key: '`', code: 'Backquote', ctrlKey: true, bubbles: true }),
      )
    })

    act(() => {
      result.current.handleKeyboardEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }))
    })

    expect(result.current.highlightedSessionId).toBe('c')

    act(() => {
      result.current.handleKeyboardEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))
    })

    expect(onSelectSession).toHaveBeenCalledWith('c')
  })
})