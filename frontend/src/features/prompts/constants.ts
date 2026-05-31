import { z } from 'zod'
import type { BadgeProps } from '@/components/ui/badge'
import {
  promptKindSchema,
  promptStatusSchema,
  targetAgentSchema,
  type PromptKind,
  type PromptStatus,
  type TargetAgent,
} from '@/api/schemas'

export const AGENT_LABELS: Record<TargetAgent, string> = {
  ClaudeCode: 'Claude Code',
  Codex: 'Codex',
}

export const KIND_LABELS: Record<PromptKind, string> = {
  General: 'Geral',
  Planning: 'Planejamento',
}

export const STATUS_LABELS: Record<PromptStatus, string> = {
  Draft: 'Rascunho',
  Ready: 'Pronto',
  Archived: 'Arquivado',
}

export const STATUS_BADGE_VARIANTS: Record<PromptStatus, BadgeProps['variant']> = {
  Draft: 'amber',
  Ready: 'green',
  Archived: 'neutral',
}

export const AGENT_OPTIONS = [
  { value: 'Codex', label: AGENT_LABELS.Codex },
  { value: 'ClaudeCode', label: AGENT_LABELS.ClaudeCode },
] satisfies Array<{ value: TargetAgent; label: string }>

export const KIND_OPTIONS = [
  { value: 'General', label: KIND_LABELS.General },
  { value: 'Planning', label: KIND_LABELS.Planning },
] satisfies Array<{ value: PromptKind; label: string }>

export const STATUS_OPTIONS = [
  { value: 'Draft', label: STATUS_LABELS.Draft },
  { value: 'Ready', label: STATUS_LABELS.Ready },
  { value: 'Archived', label: STATUS_LABELS.Archived },
] satisfies Array<{ value: PromptStatus; label: string }>

export const promptFormSchema = z.object({
  title: z.string().trim().min(3, 'Informe um titulo com pelo menos 3 caracteres.'),
  targetAgent: targetAgentSchema,
  kind: promptKindSchema,
  status: promptStatusSchema,
  content: z.string().trim().min(3, 'Escreva o prompt em markdown.'),
})

export type PromptFormValues = z.infer<typeof promptFormSchema>
