import { useCallback, useMemo, useState } from 'react'
import {
  pruneTerminalTabPreferences,
  readTerminalTabPreferences,
  type TerminalTabPreference,
  type TerminalTabPreferencesMap,
  writeTerminalTabPreferences,
} from './terminal-tab-preferences'

export function useTerminalTabPreferences(promptId: string, sessionIds: string[]) {
  const [storedPromptId, setStoredPromptId] = useState(promptId)
  const [preferences, setPreferences] = useState<TerminalTabPreferencesMap>(() =>
    readTerminalTabPreferences(promptId),
  )

  if (storedPromptId !== promptId) {
    setStoredPromptId(promptId)
    setPreferences(readTerminalTabPreferences(promptId))
  }

  const visiblePreferences = useMemo(() => {
    if (sessionIds.length === 0) {
      return preferences
    }

    return pruneTerminalTabPreferences(preferences, sessionIds)
  }, [preferences, sessionIds])

  const setSessionPreference = useCallback(
    (sessionId: string, patch: TerminalTabPreference) => {
      setPreferences((current) => {
        const next = {
          ...current,
          [sessionId]: {
            ...current[sessionId],
            ...patch,
          },
        }
        writeTerminalTabPreferences(promptId, next)
        return next
      })
    },
    [promptId],
  )

  const removeSessionPreference = useCallback(
    (sessionId: string) => {
      setPreferences((current) => {
        if (!current[sessionId]) {
          return current
        }

        const next = { ...current }
        delete next[sessionId]
        writeTerminalTabPreferences(promptId, next)
        return next
      })
    },
    [promptId],
  )

  return {
    preferences: visiblePreferences,
    setSessionPreference,
    removeSessionPreference,
  }
}