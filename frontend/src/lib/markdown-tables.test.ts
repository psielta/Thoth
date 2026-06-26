import { describe, expect, it } from 'vitest'
import { normalizeMarkdownTableBlocks } from './markdown-tables'

describe('normalizeMarkdownTableBlocks', () => {
  it('collapses blank lines between GitHub-flavored Markdown table rows', () => {
    const markdown = [
      'Use estes conceitos de forma consistente:',
      '',
      '| Conceito | Modelo/tabela | Significado |',
      '',
      '|---|---|---|',
      '',
      '| PCA | `Pca` / `pca` | Cabecalho do plano anual. |',
      '',
      '| Item manual | `PcaItem` com `id_setor` e `id_pca_ref_item = NULL` | Setor criou item sem importacao. |',
      '',
      'Regra central',
    ].join('\n')

    expect(normalizeMarkdownTableBlocks(markdown)).toBe(
      [
        'Use estes conceitos de forma consistente:',
        '',
        '| Conceito | Modelo/tabela | Significado |',
        '|---|---|---|',
        '| PCA | `Pca` / `pca` | Cabecalho do plano anual. |',
        '| Item manual | `PcaItem` com `id_setor` e `id_pca_ref_item = NULL` | Setor criou item sem importacao. |',
        '',
        'Regra central',
      ].join('\n'),
    )
  })

  it('does not collapse table-looking rows inside fenced code blocks', () => {
    const markdown = [
      '```markdown',
      '| Conceito | Modelo/tabela | Significado |',
      '',
      '|---|---|---|',
      '',
      '| PCA | Pca | Cabecalho |',
      '```',
    ].join('\n')

    expect(normalizeMarkdownTableBlocks(markdown)).toBe(markdown)
  })

  it('leaves ordinary pipe paragraphs alone when there is no table delimiter row', () => {
    const markdown = ['| esse texto | nao e tabela |', '', '| porque falta | delimitador |'].join('\n')

    expect(normalizeMarkdownTableBlocks(markdown)).toBe(markdown)
  })
})
