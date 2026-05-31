import { Markdown } from '@tiptap/markdown'
import { Extension, type MarkdownToken } from '@tiptap/core'
import type { EditorView } from '@tiptap/pm/view'
import type { JSONContent } from '@tiptap/react'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { Check, Copy, Loader2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { searchFiles, validateFileReferences } from '@/api/files'
import type { FileMention, FileSearchResult } from '@/api/schemas'
import { cn } from '@/lib/utils'
import { createFileMentionSuggestion, FileMention as FileMentionExtension } from './file-mention'

type PromptEditorProps = {
  workingDirectoryId: string
  value: string
  onChange: (value: string, mentions: FileMention[]) => void
  className?: string
  contentClassName?: string
  editorClassName?: string
  editable?: boolean
}

const fileSearchCache = new Map<string, Promise<FileSearchResult[]>>()
const plainFileMentionPattern = /(^|[\s([{"'])@([^\s@]+(?:[\\/][^\s@]+)+)/g
const trailingPathPunctuationPattern = /[)"',.;:!?]+$/
const maxPastedMentionValidationCount = 100

const MarkdownEscapeText = Extension.create({
  name: 'markdownEscapeText',
  markdownTokenName: 'escape',
  parseMarkdown: (token: MarkdownToken) => ({
    type: 'text',
    text: token.raw || token.text || '',
  }),
})

type PlainFileMentionReplacement = {
  from: number
  path: string
  to: number
}

type NormalizePlainMentionOptions = {
  alertInvalid?: boolean
  showLoading?: boolean
}

export function PromptEditor({
  workingDirectoryId,
  value,
  onChange,
  className,
  contentClassName,
  editorClassName,
  editable = true,
}: PromptEditorProps) {
  const [isValidatingMentions, setIsValidatingMentions] = useState(false)

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

  const validateAndNormalizePlainMentions = useCallback(
    async (view: EditorView, options: NormalizePlainMentionOptions = {}) => {
      const paths = collectPlainFileMentionPaths(view)
      if (!paths.length) {
        return
      }

      const pathsToValidate = paths.slice(0, maxPastedMentionValidationCount)
      if (options.showLoading) {
        setIsValidatingMentions(true)
      }

      try {
        const validations = await validateFileReferences(workingDirectoryId, pathsToValidate)
        const validPathsByKey = new Map<string, string>()
        const invalidPaths: string[] = []

        validations.forEach((validation) => {
          const key = normalizePathKey(validation.rawPath || validation.relativePath)
          if (validation.exists) {
            validPathsByKey.set(key, validation.relativePath)
            return
          }

          invalidPaths.push(validation.rawPath || validation.relativePath)
        })

        replacePlainFileMentions(view, validPathsByKey)

        if (options.alertInvalid) {
          if (paths.length > maxPastedMentionValidationCount) {
            toast.warning(`Foram validadas apenas as primeiras ${maxPastedMentionValidationCount} mencoes coladas.`)
          }

          if (invalidPaths.length) {
            const preview = invalidPaths
              .slice(0, 4)
              .map((path) => `@${path}`)
              .join('\n')
            const suffix = invalidPaths.length > 4 ? `\n... e mais ${invalidPaths.length - 4}` : ''
            toast.warning('Algumas mencoes nao existem no diretorio de trabalho.', {
              description: `${preview}${suffix}`,
            })
          }
        }
      } catch (error) {
        if (options.alertInvalid) {
          toast.error('Nao foi possivel validar as mencoes coladas.', {
            description: getErrorMessage(error),
          })
        }
      } finally {
        if (options.showLoading) {
          setIsValidatingMentions(false)
        }
      }
    },
    [workingDirectoryId],
  )

  const extensions = useMemo(
    () => [
      StarterKit,
      MarkdownEscapeText,
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
    editable,
    editorProps: {
      attributes: {
        class: cn('tiptap px-4 py-3 text-left text-sm leading-6 text-[#172126]', editorClassName),
      },
      handlePaste: (view) => {
        if (!editable) {
          return false
        }

        window.setTimeout(() => {
          void validateAndNormalizePlainMentions(view, { alertInvalid: true, showLoading: true })
        }, 0)
        return false
      },
    },
    onCreate: ({ editor: currentEditor }) => {
      if (!editable) {
        return
      }

      queueMicrotask(() => {
        void validateAndNormalizePlainMentions(currentEditor.view)
      })
    },
    onUpdate: ({ editor: currentEditor }) => {
      onChange(currentEditor.getMarkdown(), collectMentions(currentEditor.getJSON()))
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
    if (editable) {
      queueMicrotask(() => {
        void validateAndNormalizePlainMentions(editor.view)
      })
    }
  }, [editable, editor, validateAndNormalizePlainMentions, value])

  const [copied, setCopied] = useState(false)

  const handleCopy = useCallback(() => {
    if (!editor) return
    void navigator.clipboard.writeText(editor.getMarkdown()).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    })
  }, [editor])

  return (
    <div className={cn('overflow-hidden rounded-lg border border-[#cbd5c8] bg-white', className)}>
      <div className="flex items-center justify-between gap-3 border-b border-[#d9dfd5] bg-[#f7f8f6] px-4 py-2 text-xs font-medium uppercase tracking-normal text-[#66746b]">
        <span>Markdown com mencoes de arquivo</span>
        <div className="flex items-center gap-2">
          {isValidatingMentions ? (
            <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-[0.68rem] text-[#42664d]">
              <Loader2 className="h-3 w-3 animate-spin" />
              Validando mencoes
            </span>
          ) : null}
          <button
            type="button"
            onClick={handleCopy}
            title="Copiar markdown"
            className="inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[0.68rem] transition-colors hover:bg-[#e8ede5] hover:text-[#172126]"
          >
            {copied ? (
              <>
                <Check className="h-3 w-3 text-[#254632]" />
                <span className="text-[#254632]">Copiado</span>
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
      <EditorContent editor={editor} className={contentClassName} />
    </div>
  )
}

function collectPlainFileMentionPaths(view: EditorView) {
  const paths = new Map<string, string>()

  getPlainFileMentionReplacements(view).forEach((replacement) => {
    paths.set(normalizePathKey(replacement.path), replacement.path.replace(/\\/g, '/'))
  })

  return Array.from(paths.values())
}

function replacePlainFileMentions(view: EditorView, validPathsByKey: Map<string, string>) {
  const mentionType = view.state.schema.nodes.mention
  if (!mentionType) {
    return false
  }

  const replacements = getPlainFileMentionReplacements(view)
    .map((replacement) => ({
      ...replacement,
      path: validPathsByKey.get(normalizePathKey(replacement.path)) ?? '',
    }))
    .filter((replacement) => replacement.path.length > 0)

  if (!replacements.length) {
    return false
  }

  let transaction = view.state.tr
  replacements
    .sort((left, right) => right.from - left.from)
    .forEach((replacement) => {
      transaction = transaction.replaceWith(
        replacement.from,
        replacement.to,
        mentionType.create({
          id: replacement.path,
          label: replacement.path,
          mentionSuggestionChar: '@',
        }),
      )
    })

  if (!transaction.docChanged) {
    return false
  }

  view.dispatch(transaction)
  return true
}

function getPlainFileMentionReplacements(view: EditorView) {
  const replacements: PlainFileMentionReplacement[] = []

  view.state.doc.descendants((node, position) => {
    if (node.type.name === 'codeBlock') {
      return false
    }

    if (!node.isText || !node.text || node.marks.some((mark) => mark.type.name === 'code')) {
      return true
    }

    plainFileMentionPattern.lastIndex = 0

    let match: RegExpExecArray | null
    while ((match = plainFileMentionPattern.exec(node.text)) !== null) {
      const prefix = match[1] ?? ''
      const rawPath = match[2] ?? ''
      const pathWithoutTrailingPunctuation = rawPath.replace(trailingPathPunctuationPattern, '')

      if (!pathWithoutTrailingPunctuation || !/[\\/]/.test(pathWithoutTrailingPunctuation)) {
        continue
      }

      const from = position + match.index + prefix.length
      const to = from + 1 + pathWithoutTrailingPunctuation.length
      replacements.push({
        from,
        to,
        path: pathWithoutTrailingPunctuation,
      })
    }

    return true
  })

  return replacements
}

function normalizePathKey(path: string) {
  return path.trim().replace(/\\/g, '/').toLocaleLowerCase()
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
