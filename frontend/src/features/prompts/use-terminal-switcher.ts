import { useCallback, useEffect, useState } from 'react'
import {
  directionFromSwitcherNavigateKey,
  isTerminalSwitcherOpenShortcut,
  isTerminalSwitcherQuickCycleShortcut,
} from './terminal-switcher-shortcuts'

type UseTerminalSwitcherOptions = {
  enabled: boolean
  sessionIds: string[]
  activeSessionId: string | null
  onSelectSession: (sessionId: string) => void
}

function moveHighlight(sessionIds: string[], currentId: string | null, direction: 1 | -1) {
  if (sessionIds.length === 0) {
    return null
  }

  const currentIndex = currentId ? sessionIds.indexOf(currentId) : -1
  const nextIndex =
    currentIndex === -1
      ? direction === 1
        ? 0
        : sessionIds.length - 1
      : (currentIndex + direction + sessionIds.length) % sessionIds.length

  return sessionIds[nextIndex] ?? null
}

export function useTerminalSwitcher({
  enabled,
  sessionIds,
  activeSessionId,
  onSelectSession,
}: UseTerminalSwitcherOptions) {
  const [open, setOpen] = useState(false)
  const [highlightedSessionId, setHighlightedSessionId] = useState<string | null>(null)

  const closeSwitcher = useCallback(
    (applySelection: boolean) => {
      setOpen(false)
      setHighlightedSessionId((current) => {
        if (applySelection && current) {
          onSelectSession(current)
        }
        return null
      })
    },
    [onSelectSession],
  )

  const highlightNext = useCallback(
    (direction: 1 | -1, openOverlay: boolean) => {
      const baseId = open ? highlightedSessionId ?? activeSessionId : activeSessionId
      const nextId = moveHighlight(sessionIds, baseId, direction)

      if (!nextId) {
        return
      }

      if (openOverlay) {
        setHighlightedSessionId(nextId)
        setOpen(true)
        return
      }

      onSelectSession(nextId)
    },
    [activeSessionId, highlightedSessionId, onSelectSession, open, sessionIds],
  )

  const handleKeyboardEvent = useCallback(
    (event: KeyboardEvent): boolean => {
      if (!enabled || sessionIds.length < 2) {
        return false
      }

      if (open) {
        if (event.key === 'Enter') {
          event.preventDefault()
          event.stopPropagation()
          closeSwitcher(true)
          return true
        }

        if (event.key === 'Escape') {
          event.preventDefault()
          event.stopPropagation()
          closeSwitcher(false)
          return true
        }

        const overlayDirection = directionFromSwitcherNavigateKey(event.key, event.shiftKey)
        if (overlayDirection !== null && !event.ctrlKey && !event.altKey && !event.metaKey) {
          event.preventDefault()
          event.stopPropagation()
          highlightNext(overlayDirection, true)
          return true
        }
      }

      const quickCycleDirection = isTerminalSwitcherQuickCycleShortcut(event)
      if (quickCycleDirection !== null) {
        event.preventDefault()
        event.stopPropagation()
        highlightNext(quickCycleDirection, false)
        return true
      }

      if (isTerminalSwitcherOpenShortcut(event)) {
        event.preventDefault()
        event.stopPropagation()
        highlightNext(1, true)
        return true
      }

      return false
    },
    [closeSwitcher, enabled, highlightNext, open, sessionIds.length],
  )

  useEffect(() => {
    if (!enabled || sessionIds.length < 2) {
      return
    }

    const onKeyDown = (event: KeyboardEvent) => {
      handleKeyboardEvent(event)
    }

    window.addEventListener('keydown', onKeyDown, true)

    return () => {
      window.removeEventListener('keydown', onKeyDown, true)
    }
  }, [enabled, handleKeyboardEvent, sessionIds.length])

  return {
    switcherOpen: open,
    highlightedSessionId: highlightedSessionId ?? activeSessionId ?? sessionIds[0] ?? null,
    handleKeyboardEvent,
  }
}