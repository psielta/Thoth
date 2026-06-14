import { useCallback } from 'react'
import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { openFileInVsCode } from '@/api/files'

/**
 * Abre um arquivo do workspace no VS Code (workspace aberto com o arquivo em foco).
 * Retorna um callback estavel para uso no menu de contexto do explorador.
 */
export function useOpenFileInVsCode() {
  const { mutate } = useMutation({
    mutationFn: ({ workingDirectoryId, relativePath }: { workingDirectoryId: string; relativePath: string }) =>
      openFileInVsCode(workingDirectoryId, relativePath),
    onSuccess: () => toast.success('VS Code aberto no arquivo.'),
    onError: (error) => toast.error(getErrorMessage(error)),
  })

  return useCallback(
    (workingDirectoryId: string, relativePath: string) => mutate({ workingDirectoryId, relativePath }),
    [mutate],
  )
}
