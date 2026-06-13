import { describe, expect, it } from 'vitest'
import {
  isTerminalSwitcherOpenShortcut,
  isTerminalSwitcherQuickCycleShortcut,
} from './terminal-switcher-shortcuts'

describe('terminal-switcher-shortcuts', () => {
  it('detects Ctrl+` open shortcut', () => {
    expect(
      isTerminalSwitcherOpenShortcut(
        new KeyboardEvent('keydown', { key: '`', code: 'Backquote', ctrlKey: true }),
      ),
    ).toBe(true)
    expect(
      isTerminalSwitcherOpenShortcut(new KeyboardEvent('keydown', { key: 'Tab', ctrlKey: true })),
    ).toBe(false)
  })

  it('detects Ctrl+Alt+arrow quick cycle shortcuts', () => {
    expect(
      isTerminalSwitcherQuickCycleShortcut(
        new KeyboardEvent('keydown', { key: 'ArrowRight', ctrlKey: true, altKey: true }),
      ),
    ).toBe(1)
    expect(
      isTerminalSwitcherQuickCycleShortcut(
        new KeyboardEvent('keydown', { key: 'ArrowLeft', ctrlKey: true, altKey: true }),
      ),
    ).toBe(-1)
  })
})