import type { PromptKind, PromptStatus, TargetAgent } from './schemas'

export type PromptFilters = {
  workingDirectoryId?: string
  status?: PromptStatus
  agent?: TargetAgent
  kind?: PromptKind
  q?: string
}

export const queryKeys = {
  workingDirectories: {
    all: ['working-directories'] as const,
    detail: (id: string) => ['working-directories', id] as const,
  },
  files: {
    search: (workingDirectoryId: string, query: string, limit: number) =>
      ['files', 'search', workingDirectoryId, query, limit] as const,
  },
  prompts: {
    all: ['prompts'] as const,
    list: (filters: PromptFilters) => ['prompts', 'list', filters] as const,
    detail: (id: string) => ['prompts', id] as const,
    versions: (id: string) => ['prompts', id, 'versions'] as const,
  },
}
