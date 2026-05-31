import { useQueries } from '@tanstack/react-query'
import { getLinkedDocumentContent } from '@/api/linked-documents'
import { queryKeys } from '@/api/query-keys'

export function useLinkedPlanCompare(
  documentId: string,
  a?: number,
  b?: number,
  isOpen = false,
) {
  const results = useQueries({
    queries: [a, b].map((v) => ({
      queryKey: queryKeys.linkedDocuments.content(documentId, v),
      queryFn: () => getLinkedDocumentContent(documentId, v!),
      enabled: isOpen && v != null,
    })),
  })

  return {
    contents: results.map((r) => r.data?.content ?? ''),
    isLoading: results.some((r) => r.isLoading),
    error: results.find((r) => r.error)?.error ?? null,
  }
}
