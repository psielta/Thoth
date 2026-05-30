import { z } from 'zod'

export const targetAgentSchema = z.enum(['ClaudeCode', 'Codex'])
export const promptKindSchema = z.enum(['General', 'Planning'])
export const promptStatusSchema = z.enum(['Draft', 'Ready', 'Archived'])

export type TargetAgent = z.infer<typeof targetAgentSchema>
export type PromptKind = z.infer<typeof promptKindSchema>
export type PromptStatus = z.infer<typeof promptStatusSchema>

export const fileMentionSchema = z.object({
  id: z.string().min(1),
  label: z.string().nullable().optional(),
  relativePath: z.string().optional(),
})

export const workingDirectorySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  absolutePath: z.string(),
  respectGitignore: z.boolean(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
})

export const validatePathResponseSchema = z.object({
  isValid: z.boolean(),
  canonicalPath: z.string().nullable(),
  error: z.string().nullable(),
})

export const fileSearchResultSchema = z.object({
  relativePath: z.string(),
  fileName: z.string(),
  isDirectory: z.boolean(),
  score: z.number(),
})

export const fileReferenceValidationSchema = z.object({
  rawPath: z.string(),
  relativePath: z.string(),
  exists: z.boolean(),
  isDirectory: z.boolean(),
  error: z.string().nullable(),
})

export const promptSchema = z.object({
  id: z.string().uuid(),
  workingDirectoryId: z.string().uuid(),
  title: z.string(),
  content: z.string(),
  targetAgent: targetAgentSchema,
  kind: promptKindSchema,
  status: promptStatusSchema,
  currentVersion: z.number(),
  rowVersion: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  mentions: z.array(fileMentionSchema),
})

export const promptVersionSchema = z.object({
  id: z.string().uuid(),
  promptId: z.string().uuid(),
  versionNumber: z.number(),
  title: z.string(),
  content: z.string(),
  targetAgent: targetAgentSchema,
  kind: promptKindSchema,
  status: promptStatusSchema,
  changeNote: z.string().nullable(),
  createdAtUtc: z.string(),
})

export type FileMention = z.infer<typeof fileMentionSchema>
export type WorkingDirectory = z.infer<typeof workingDirectorySchema>
export type ValidatePathResponse = z.infer<typeof validatePathResponseSchema>
export type FileSearchResult = z.infer<typeof fileSearchResultSchema>
export type FileReferenceValidation = z.infer<typeof fileReferenceValidationSchema>
export type Prompt = z.infer<typeof promptSchema>
export type PromptVersion = z.infer<typeof promptVersionSchema>

export const workingDirectoryListSchema = z.array(workingDirectorySchema)
export const fileSearchResultListSchema = z.array(fileSearchResultSchema)
export const fileReferenceValidationListSchema = z.array(fileReferenceValidationSchema)
export const promptListSchema = z.array(promptSchema)
export const promptVersionListSchema = z.array(promptVersionSchema)
