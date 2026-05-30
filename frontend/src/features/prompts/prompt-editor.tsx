import { Markdown } from '@tiptap/markdown'
import type { JSONContent } from '@tiptap/react'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { useCallback, useEffect, useMemo } from 'react'
import { searchFiles } from '@/api/files'
import type { FileMention, FileSearchResult } from '@/api/schemas'
import { createFileMentionSuggestion, FileMention as FileMentionExtension } from './file-mention'

type PromptEditorProps = {
  workingDirectoryId: string
  value: string
  onChange: (value: string, mentions: FileMention[]) => void
}

const fileSearchCache = new Map<string, Promise<FileSearchResult[]>>()

export function PromptEditor({ workingDirectoryId, value, onChange }: PromptEditorProps) {
  const searchMentions = useCallback(
    (query: string) => {
      const normalizedQuery = query.trim().replace(/^@+/, '')
      const cacheKey = `${workingDirectoryId}:${normalizedQuery}`
      const cached = fileSearchCache.get(cacheKey)
      if (cached) {
        return cached
      }

      const request = searchFiles(workingDirectoryId, normalizedQuery, 20).catch((error: unknown) => {
        fileSearchCache.delete(cacheKey)
        throw error
      })

      if (fileSearchCache.size > 200) {
        fileSearchCache.clear()
      }

      fileSearchCache.set(cacheKey, request)
      return request
    },
    [workingDirectoryId],
  )

  const extensions = useMemo(
    () => [
      StarterKit,
      FileMentionExtension.configure({
        HTMLAttributes: {
          class: 'file-mention',
        },
        renderText: ({ node }) => `@${node.attrs.id}`,
        renderHTML: ({ node }) => ['span', { 'data-type': 'mention', class: 'file-mention' }, `@${node.attrs.id}`],
        suggestion: createFileMentionSuggestion(searchMentions),
      }),
      Markdown,
    ],
    [searchMentions],
  )

  const editor = useEditor({
    extensions,
    content: value || '',
    contentType: 'markdown',
    editorProps: {
      attributes: {
        class: 'tiptap px-4 py-3 text-left text-sm leading-6 text-[#172126]',
      },
    },
    onUpdate: ({ editor: currentEditor }) => {
      onChange(currentEditor.getMarkdown(), collectMentions(currentEditor.getJSON()))
    },
  })

  useEffect(() => {
    if (!editor || editor.getMarkdown() === value) {
      return
    }

    editor.commands.setContent(value || '', { contentType: 'markdown' })
  }, [editor, value])

  return (
    <div className="overflow-hidden rounded-lg border border-[#cbd5c8] bg-white">
      <div className="border-b border-[#d9dfd5] bg-[#f7f8f6] px-4 py-2 text-xs font-medium uppercase tracking-normal text-[#66746b]">
        Markdown com mencoes de arquivo
      </div>
      <EditorContent editor={editor} />
    </div>
  )
}

function collectMentions(document: JSONContent): FileMention[] {
  const mentions = new Map<string, FileMention>()

  const visit = (node: JSONContent) => {
    if (node.type === 'mention') {
      const id = typeof node.attrs?.id === 'string' ? node.attrs.id : ''
      const label = typeof node.attrs?.label === 'string' ? node.attrs.label : id
      if (id) {
        mentions.set(id, { id, label, relativePath: id })
      }
    }

    node.content?.forEach(visit)
  }

  visit(document)

  return Array.from(mentions.values())
}
