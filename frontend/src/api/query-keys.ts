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
  promptTemplates: {
    all: ['prompt-templates'] as const,
    draft: (linkedDocumentId: string, templateKey: string) =>
      ['prompt-templates', 'draft', linkedDocumentId, templateKey] as const,
  },
  linkedDocuments: {
    all: ['linked-documents'] as const,
    forPrompt: (promptId: string) => ['linked-documents', 'prompt', promptId] as const,
    detail: (id: string) => ['linked-documents', id] as const,
    contentRoot: (id: string) => ['linked-documents', id, 'content'] as const,
    content: (id: string, version?: number) =>
      ['linked-documents', id, 'content', version ?? 'latest'] as const,
    versions: (id: string) => ['linked-documents', id, 'versions'] as const,
  },
}
