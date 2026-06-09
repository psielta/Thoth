import { describe, expect, it } from 'vitest'
import {
  buildPromptSeedFromNote,
  PROMPT_CONTENT_MAX_LENGTH,
  PROMPT_TITLE_MAX_LENGTH,
} from './note-prompt-seed'

describe('buildPromptSeedFromNote', () => {
  it('uses the note title and content as the prompt seed', () => {
    const seed = buildPromptSeedFromNote({ title: ' Minha nota ', contentMarkdown: '# Conteudo' })

    expect(seed).toEqual({ title: 'Minha nota', content: '# Conteudo', truncated: false })
  })

  it('caps the title at the backend limit', () => {
    const seed = buildPromptSeedFromNote({
      title: 'a'.repeat(PROMPT_TITLE_MAX_LENGTH + 50),
      contentMarkdown: 'conteudo',
    })

    expect(seed.title).toHaveLength(PROMPT_TITLE_MAX_LENGTH)
  })

  it('truncates oversized content and flags the cut', () => {
    const seed = buildPromptSeedFromNote({
      title: 'Nota',
      contentMarkdown: 'x'.repeat(PROMPT_CONTENT_MAX_LENGTH + 10),
    })

    expect(seed.truncated).toBe(true)
    expect(seed.content).toHaveLength(PROMPT_CONTENT_MAX_LENGTH)
  })
})
