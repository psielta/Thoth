import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createDiagram,
  deleteDiagram,
  getDiagram,
  listDiagrams,
  setDiagramArchived,
  updateDiagram,
  type CreateDiagramPayload,
  type UpdateDiagramPayload,
} from '@/api/diagrams'
import { queryKeys, type DiagramFilters } from '@/api/query-keys'

export function useDiagrams(filters: DiagramFilters) {
  return useQuery({
    queryKey: queryKeys.diagrams.list(filters),
    queryFn: () => listDiagrams(filters),
  })
}

export function useDiagram(id: string | null) {
  return useQuery({
    queryKey: id ? queryKeys.diagrams.detail(id) : ['diagrams', 'none'],
    queryFn: () => getDiagram(id as string),
    enabled: Boolean(id),
  })
}

export function useDiagramMutations() {
  const queryClient = useQueryClient()
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.diagrams.all })
  }

  const create = useMutation({
    mutationFn: (payload: CreateDiagramPayload) => createDiagram(payload),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateDiagramPayload }) => updateDiagram(id, payload),
    onSuccess: invalidate,
  })

  const archive = useMutation({
    mutationFn: ({ id, isArchived }: { id: string; isArchived: boolean }) => setDiagramArchived(id, isArchived),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteDiagram(id),
    onSuccess: invalidate,
  })

  return { create, update, archive, remove }
}
