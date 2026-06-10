import { useQuery } from '@tanstack/react-query'
import { getGitDiff, getGitOriginalFile, getGitStatus } from '@/api/git'
import { queryKeys } from '@/api/query-keys'

export function useGitStatus(workingDirectoryId: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.git.status(workingDirectoryId),
    queryFn: () => getGitStatus(workingDirectoryId),
    enabled: Boolean(workingDirectoryId) && enabled,
    staleTime: 5_000,
  })
}

export function useGitOriginalFile(workingDirectoryId: string, path: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.git.originalFile(workingDirectoryId, path),
    queryFn: () => getGitOriginalFile(workingDirectoryId, path),
    enabled: Boolean(workingDirectoryId) && Boolean(path) && enabled,
    staleTime: 0,
  })
}

export function useGitDiff(workingDirectoryId: string, path: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.git.diff(workingDirectoryId, path),
    queryFn: () => getGitDiff(workingDirectoryId, path),
    enabled: Boolean(workingDirectoryId) && Boolean(path) && enabled,
    staleTime: 0,
  })
}
