import { Editor, type JSONContent } from '@tiptap/core'
import { afterEach, describe, expect, it } from 'vitest'
import { createMarkdownEditorExtensions } from './tiptap-markdown-extensions'

const editors: Editor[] = []

function createEditor(content = '') {
  const editor = new Editor({
    extensions: createMarkdownEditorExtensions(),
    content,
    contentType: 'markdown',
  })
  editors.push(editor)
  return editor
}

function countNodes(node: JSONContent, type: string): number {
  const current = node.type === type ? 1 : 0
  return current + (node.content ?? []).reduce((total, child) => total + countNodes(child, type), 0)
}

function findNode(node: JSONContent, type: string): JSONContent | null {
  if (node.type === type) {
    return node
  }

  for (const child of node.content ?? []) {
    const found = findNode(child, type)
    if (found) {
      return found
    }
  }

  return null
}

function tableColumnCounts(table: JSONContent) {
  return (table.content ?? []).map((row) => row.content?.length ?? 0)
}

describe('createMarkdownEditorExtensions', () => {
  afterEach(() => {
    while (editors.length > 0) {
      editors.pop()?.destroy()
    }
  })

  it('round-trips GitHub-flavored Markdown tables', () => {
    const source = [
      '| Conceito | Modelo/tabela | Significado |',
      '|---|---|---|',
      '| PCA | `Pca` / `pca` | Cabecalho do plano anual. |',
      '| Item de referencia | `PcaRefItem` / `pca_ref_item` | Linha importada, imutavel, usada como base/comparacao. |',
      '| Preenchimento setorial | `PcaItem` / `pca_item` com `id_setor` | Demanda real preenchida por um setor. |',
    ].join('\n')

    const editor = createEditor(source)
    const json = editor.getJSON()

    expect(countNodes(json, 'table')).toBe(1)
    expect(countNodes(json, 'tableRow')).toBe(4)
    expect(countNodes(json, 'tableHeader')).toBe(3)
    expect(countNodes(json, 'tableCell')).toBe(9)

    const output = editor.getMarkdown()
    expect(output).toContain('| Conceito')
    expect(output).toContain('| PCA')
    expect(output).toContain('`Pca` / `pca`')
    expect(output).toContain('`PcaItem` / `pca_item` com `id_setor`')

    const reloaded = createEditor(output)
    expect(countNodes(reloaded.getJSON(), 'table')).toBe(1)
  })

  it('supports the table commands used by the toolbar', () => {
    const editor = createEditor()

    expect(editor.commands.insertTable({ rows: 3, cols: 3, withHeaderRow: true })).toBe(true)

    let table = findNode(editor.getJSON(), 'table')
    expect(table).not.toBeNull()
    expect(countNodes(editor.getJSON(), 'tableRow')).toBe(3)
    expect(tableColumnCounts(table!)).toEqual([3, 3, 3])

    expect(editor.commands.addRowAfter()).toBe(true)
    table = findNode(editor.getJSON(), 'table')
    expect(countNodes(editor.getJSON(), 'tableRow')).toBe(4)
    expect(tableColumnCounts(table!)).toEqual([3, 3, 3, 3])

    expect(editor.commands.addColumnAfter()).toBe(true)
    table = findNode(editor.getJSON(), 'table')
    expect(tableColumnCounts(table!)).toEqual([4, 4, 4, 4])

    expect(editor.commands.deleteColumn()).toBe(true)
    table = findNode(editor.getJSON(), 'table')
    expect(tableColumnCounts(table!)).toEqual([3, 3, 3, 3])

    expect(countNodes(editor.getJSON(), 'tableHeader')).toBe(3)
    expect(editor.commands.toggleHeaderRow()).toBe(true)
    expect(countNodes(editor.getJSON(), 'tableHeader')).toBe(0)

    expect(editor.commands.deleteTable()).toBe(true)
    expect(countNodes(editor.getJSON(), 'table')).toBe(0)
  })
})
