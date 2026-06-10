import { useEffect } from 'react'
import { usePromptHub } from '@/realtime/prompt-hub'

export function useFileSubscription(
  workingDirectoryId: string | undefined,
  relativePath: string | undefined,
  enabled = true,
) {
  const { joinFile, leaveFile } = usePromptHub()

  useEffect(() => {
    if (!enabled || !workingDirectoryId || !relativePath) {
      return
    }

    joinFile(workingDirectoryId, relativePath)
    return () => leaveFile(workingDirectoryId, relativePath)
  }, [enabled, joinFile, leaveFile, relativePath, workingDirectoryId])
}
