import { Extension, type AnyExtension, type MarkdownToken } from '@tiptap/core'
import { TableKit } from '@tiptap/extension-table'
import { Markdown } from '@tiptap/markdown'
import StarterKit from '@tiptap/starter-kit'

export const MarkdownEscapeText = Extension.create({
  name: 'markdownEscapeText',
  markdownTokenName: 'escape',
  parseMarkdown: (token: MarkdownToken) => ({
    type: 'text',
    text: token.raw || token.text || '',
  }),
})

export function createMarkdownEditorExtensions(extraExtensions: AnyExtension[] = []) {
  return [
    StarterKit,
    MarkdownEscapeText,
    TableKit.configure({
      table: {
        renderWrapper: true,
      },
    }),
    ...extraExtensions,
    Markdown.configure({
      markedOptions: {
        gfm: true,
        breaks: false,
      },
    }),
  ]
}
