export const TERMINAL_SWITCHER_OPEN_LABEL = 'Ctrl+`'
export const TERMINAL_SWITCHER_CYCLE_LABEL = 'Ctrl+Alt+←/→'

export function isTerminalSwitcherOpenShortcut(event: KeyboardEvent) {
  return (
    event.type === 'keydown' &&
    event.ctrlKey &&
    !event.altKey &&
    !event.metaKey &&
    !event.shiftKey &&
    (event.code === 'Backquote' || event.key === '`' || event.key === 'Dead')
  )
}

export function isTerminalSwitcherQuickCycleShortcut(event: KeyboardEvent): 1 | -1 | null {
  if (event.type !== 'keydown' || !event.ctrlKey || !event.altKey || event.metaKey || event.shiftKey) {
    return null
  }

  if (event.key === 'ArrowRight') {
    return 1
  }

  if (event.key === 'ArrowLeft') {
    return -1
  }

  return null
}

export function directionFromSwitcherNavigateKey(key: string, shiftKey: boolean): 1 | -1 | null {
  if (key === 'ArrowUp' || key === 'ArrowLeft') {
    return -1
  }

  if (key === 'ArrowDown' || key === 'ArrowRight') {
    return 1
  }

  if (key === 'Tab') {
    return shiftKey ? -1 : 1
  }

  return null
}