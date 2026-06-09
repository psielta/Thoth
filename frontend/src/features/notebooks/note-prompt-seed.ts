import type { Note } from '@/api/schemas'

// Limites do backend (CreatePromptValidator): titulo 220, conteudo 200_000.
export const PROMPT_TITLE_MAX_LENGTH = 220
export const PROMPT_CONTENT_MAX_LENGTH = 200_000

export type NotePromptSeed = {
  title: string
  content: string
  truncated: boolean
}

/**
 * Monta os valores iniciais do drawer de novo prompt a partir de uma nota.
 * Hoje notas sao limitadas a 100k caracteres e sempre cabem no prompt, mas o
 * recorte controlado garante que conteudos maiores nunca falhem em silencio.
 */
export function buildPromptSeedFromNote(note: Pick<Note, 'title' | 'contentMarkdown'>): NotePromptSeed {
  const title = note.title.trim().slice(0, PROMPT_TITLE_MAX_LENGTH)
  const truncated = note.contentMarkdown.length > PROMPT_CONTENT_MAX_LENGTH

  return {
    title,
    content: truncated ? note.contentMarkdown.slice(0, PROMPT_CONTENT_MAX_LENGTH) : note.contentMarkdown,
    truncated,
  }
}
