import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/api/query-keys'
import {
  createNotebook,
  deleteNotebook,
  listNotebooks,
  setNotebookArchived,
  updateNotebook,
  type NotebookPayload,
} from '@/api/notebooks'

export function useNotebooks(includeArchived: boolean) {
  return useQuery({
    queryKey: queryKeys.notebooks.list(includeArchived),
    queryFn: () => listNotebooks(includeArchived),
  })
}

export function useNotebookMutations() {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.notebooks.all })

  const create = useMutation({
    mutationFn: (payload: NotebookPayload) => createNotebook(payload),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: NotebookPayload }) => updateNotebook(id, payload),
    onSuccess: invalidate,
  })

  const archive = useMutation({
    mutationFn: ({ id, isArchived }: { id: string; isArchived: boolean }) => setNotebookArchived(id, isArchived),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteNotebook(id),
    onSuccess: invalidate,
  })

  return { create, update, archive, remove }
}
