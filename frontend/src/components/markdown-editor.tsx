import { EditorContent, useEditor } from '@tiptap/react'
import { Check, Copy } from 'lucide-react'
import { forwardRef, useCallback, useEffect, useImperativeHandle, useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import { createMarkdownEditorExtensions } from './tiptap-markdown-extensions'
import { TiptapTableToolbar } from './tiptap-table-toolbar'

type MarkdownEditorProps = {
  value: string
  onChange: (value: string) => void
  label?: string
  className?: string
  contentClassName?: string
  editorClassName?: string
  editable?: boolean
}

export type MarkdownEditorHandle = {
  /** Insert Markdown at the current cursor position, keeping `onChange` in sync. */
  insertMarkdown: (markdown: string) => void
}

/**
 * Generic TipTap Markdown editor. Unlike `PromptEditor` it carries no workspace
 * coupling or `@arquivo` mentions, so it can be reused anywhere plain Markdown
 * editing is needed (e.g. notebooks). The editor reuses the shared `.tiptap`
 * styles defined in `index.css`.
 */
export const MarkdownEditor = forwardRef<MarkdownEditorHandle, MarkdownEditorProps>(function MarkdownEditor(
  {
    value,
    onChange,
    label = 'Markdown',
    className,
    contentClassName,
    editorClassName,
    editable = true,
  },
  ref,
) {
  const extensions = useMemo(() => createMarkdownEditorExtensions(), [])

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

  useImperativeHandle(
    ref,
    () => ({
      insertMarkdown: (markdown: string) => {
        if (!editor || !markdown) {
          return
        }
        editor.chain().focus().insertContent(markdown, { contentType: 'markdown' }).run()
      },
    }),
    [editor],
  )

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
      <div className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b border-border bg-background px-4 py-2 text-xs font-medium uppercase tracking-normal text-muted-foreground">
        <span>{label}</span>
        <div className="flex flex-wrap items-center justify-end gap-2">
          <TiptapTableToolbar editor={editor} editable={editable} />
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
      </div>
      <EditorContent editor={editor} className={cn('min-h-0 flex-1 overflow-y-auto', contentClassName)} />
    </div>
  )
})
