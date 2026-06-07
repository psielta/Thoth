import { Markdown } from '@tiptap/markdown'
import { Extension, type MarkdownToken } from '@tiptap/core'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { Check, Copy } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { cn } from '@/lib/utils'

type MarkdownEditorProps = {
  value: string
  onChange: (value: string) => void
  label?: string
  className?: string
  contentClassName?: string
  editorClassName?: string
  editable?: boolean
}

/**
 * Generic TipTap Markdown editor. Unlike `PromptEditor` it carries no workspace
 * coupling or `@arquivo` mentions, so it can be reused anywhere plain Markdown
 * editing is needed (e.g. notebooks). The editor reuses the shared `.tiptap`
 * styles defined in `index.css`.
 */
const MarkdownEscapeText = Extension.create({
  name: 'markdownEscapeText',
  markdownTokenName: 'escape',
  parseMarkdown: (token: MarkdownToken) => ({
    type: 'text',
    text: token.raw || token.text || '',
  }),
})

export function MarkdownEditor({
  value,
  onChange,
  label = 'Markdown',
  className,
  contentClassName,
  editorClassName,
  editable = true,
}: MarkdownEditorProps) {
  const extensions = useMemo(() => [StarterKit, MarkdownEscapeText, Markdown], [])

  const editor = useEditor({
    extensions,
    content: value || '',
    contentType: 'markdown',
    editable,
    editorProps: {
      attributes: {
        class: cn('tiptap px-4 py-3 text-left text-sm leading-6 text-foreground', editorClassName),
      },
    },
    onUpdate: ({ editor: currentEditor }) => {
      onChange(currentEditor.getMarkdown())
    },
  })

  useEffect(() => {
    editor?.setEditable(editable)
  }, [editable, editor])

  useEffect(() => {
    if (!editor || editor.getMarkdown() === value) {
      return
    }

    editor.commands.setContent(value || '', { contentType: 'markdown' })
  }, [editor, value])

  const [copied, setCopied] = useState(false)

  const handleCopy = useCallback(() => {
    if (!editor) {
      return
    }
    void navigator.clipboard.writeText(editor.getMarkdown()).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }, [editor])

  return (
    <div className={cn('flex flex-col overflow-hidden rounded-lg border border-input bg-card', className)}>
      <div className="flex shrink-0 items-center justify-between gap-3 border-b border-border bg-background px-4 py-2 text-xs font-medium uppercase tracking-normal text-muted-foreground">
        <span>{label}</span>
        <button
          type="button"
          onClick={handleCopy}
          title="Copiar markdown"
          className="inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[0.68rem] transition-colors hover:bg-secondary hover:text-foreground"
        >
          {copied ? (
            <>
              <Check className="h-3 w-3 text-primary" />
              <span className="text-primary">Copiado</span>
            </>
          ) : (
            <>
              <Copy className="h-3 w-3" />
              Copiar
            </>
          )}
        </button>
      </div>
      <EditorContent editor={editor} className={cn('min-h-0 flex-1 overflow-y-auto', contentClassName)} />
    </div>
  )
}
