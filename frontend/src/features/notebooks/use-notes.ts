import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys, type NoteFilters } from '@/api/query-keys'
import {
  createNote,
  deleteNote,
  listNotes,
  setNoteArchived,
  setNotePinned,
  updateNote,
  type CreateNotePayload,
  type UpdateNotePayload,
} from '@/api/notes'

export function useNotes(filters: NoteFilters, enabled = true) {
  return useQuery({
    queryKey: queryKeys.notes.list(filters),
    queryFn: () => listNotes(filters),
    enabled,
  })
}

export function useNoteMutations() {
  const queryClient = useQueryClient()
  // Notes feed both the note list and the notebook note counts, so invalidate both.
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.notes.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.notebooks.all })
  }

  const create = useMutation({
    mutationFn: (payload: CreateNotePayload) => createNote(payload),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateNotePayload }) => updateNote(id, payload),
    onSuccess: invalidate,
  })

  const pin = useMutation({
    mutationFn: ({ id, isPinned }: { id: string; isPinned: boolean }) => setNotePinned(id, isPinned),
    onSuccess: invalidate,
  })

  const archive = useMutation({
    mutationFn: ({ id, isArchived }: { id: string; isArchived: boolean }) => setNoteArchived(id, isArchived),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteNote(id),
    onSuccess: invalidate,
  })

  return { create, update, pin, archive, remove }
}
