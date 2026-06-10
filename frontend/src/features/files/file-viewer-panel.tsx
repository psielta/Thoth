import { AlertTriangle, BookOpen, Code2, Copy, FileCode2, Loader2 } from 'lucide-react'
import { lazy, Suspense, useMemo } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import { toast } from 'sonner'
import { getErrorMessage } from '@/api/client'
import { useTheme } from '@/components/theme/theme-provider'
import { useLocalStorage } from '@/hooks/use-local-storage'
import { cn } from '@/lib/utils'
import { extensionToLanguage } from './extension-to-language'
import { useFileContent } from './use-file-queries'
import { useFileSubscription } from './use-file-subscription'

const MonacoEditor = lazy(async () => {
  await import('./monaco-setup')
  return import('@monaco-editor/react')
})

type FileViewerPanelProps = {
  workingDirectoryId: string
  relativePath: string
  className?: string
  inline?: boolean
}

const byteFormatter = new Intl.NumberFormat('pt-BR')

// Preferencia compartilhada entre todas as superficies do viewer (explorer
// inline, modo expandido e drawer), persistida no mesmo padrao das demais
// chaves de arquivos.
const MARKDOWN_VIEW_STORAGE_KEY = 'prompt-tasks:files:markdown-view'

type ToolbarIconButtonProps = {
  onClick: () => void
  title: string
  ariaLabel: string
  active?: boolean
  children: React.ReactNode
}

function ToolbarIconButton({ onClick, title, ariaLabel, active, children }: ToolbarIconButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={ariaLabel}
      aria-pressed={active}
      className={cn(
        'rounded p-1 transition-colors hover:bg-secondary hover:text-foreground',
        active ? 'bg-secondary text-foreground' : 'text-muted-foreground',
      )}
    >
      {children}
    </button>
  )
}

export function FileViewerPanel({ workingDirectoryId, relativePath, className, inline = false }: FileViewerPanelProps) {
  const contentQuery = useFileContent(workingDirectoryId, relativePath)
  useFileSubscription(workingDirectoryId, relativePath)
  const { resolvedTheme } = useTheme()

  const [markdownViewPref, setMarkdownViewPref] = useLocalStorage(MARKDOWN_VIEW_STORAGE_KEY, 'code')

  const language = useMemo(() => {
    const extension = relativePath.includes('.') ? relativePath.slice(relativePath.lastIndexOf('.')) : null
    return extensionToLanguage(extension)
  }, [relativePath])

  const fileName = relativePath.split('/').pop() || relativePath
  const isMarkdown = language === 'markdown'
  const hasTextContent = Boolean(contentQuery.data && !contentQuery.data.isBinary)
  const showMarkdownPreview = isMarkdown && hasTextContent && markdownViewPref === 'preview'

  const copyRelativePath = async () => {
    try {
      if (!navigator.clipboard?.writeText) {
        throw new Error('Área de transferência indisponível neste navegador.')
      }

      await navigator.clipboard.writeText(relativePath)
      toast.success('Caminho relativo copiado.')
    } catch (error) {
      toast.error(getErrorMessage(error))
    }
  }

  return (
    <section
      className={cn(
        'grid min-h-0 grid-rows-[auto_minmax(0,1fr)] overflow-hidden rounded-lg border border-border bg-card',
        className,
      )}
    >
      <div className="flex min-w-0 items-center justify-between gap-2 border-b border-border px-3 py-2">
        <div className="flex min-w-0 items-center gap-2">
          <FileCode2 className="h-4 w-4 shrink-0 text-primary" />
          <div className="min-w-0">
            <p className="truncate font-mono text-sm font-medium text-foreground" title={relativePath}>
              {fileName}
            </p>
            {!inline ? <p className="truncate text-xs text-muted-foreground">{relativePath}</p> : null}
          </div>
        </div>
        <div className="flex shrink-0 flex-wrap items-center justify-end gap-1.5">
          {isMarkdown && hasTextContent ? (
            <div
              role="group"
              aria-label="Modo de visualizacao do markdown"
              className="flex shrink-0 items-center overflow-hidden rounded-md border border-border"
            >
              <button
                type="button"
                onClick={() => setMarkdownViewPref('code')}
                aria-pressed={!showMarkdownPreview}
                title="Ver codigo-fonte no editor"
                className={cn(
                  'flex items-center gap-1 px-2 py-1 text-xs font-medium transition-colors',
                  showMarkdownPreview
                    ? 'text-muted-foreground hover:bg-secondary hover:text-foreground'
                    : 'bg-secondary text-foreground',
                )}
              >
                <Code2 className="h-3.5 w-3.5" />
                Codigo
              </button>
              <button
                type="button"
                onClick={() => setMarkdownViewPref('preview')}
                aria-pressed={showMarkdownPreview}
                title="Ver markdown renderizado"
                className={cn(
                  'flex items-center gap-1 px-2 py-1 text-xs font-medium transition-colors',
                  showMarkdownPreview
                    ? 'bg-secondary text-foreground'
                    : 'text-muted-foreground hover:bg-secondary hover:text-foreground',
                )}
              >
                <BookOpen className="h-3.5 w-3.5" />
                Visual
              </button>
            </div>
          ) : null}

          {contentQuery.data ? (
            <span className="hidden text-xs text-muted-foreground sm:inline">
              {byteFormatter.format(contentQuery.data.sizeBytes)} bytes
            </span>
          ) : null}
          <ToolbarIconButton
            onClick={() => void copyRelativePath()}
            title="Copiar caminho relativo"
            ariaLabel="Copiar caminho relativo"
          >
            <Copy className="h-3.5 w-3.5" />
          </ToolbarIconButton>
        </div>
      </div>

      <div className="grid min-h-0 grid-rows-[minmax(0,1fr)_auto]">
        <div className="min-h-0 overflow-hidden">
          {contentQuery.isLoading ? (
            <div className="flex h-full min-h-48 items-center justify-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Carregando arquivo
            </div>
          ) : null}

          {contentQuery.isError ? (
            <div className="flex h-full min-h-48 items-center justify-center px-4 text-sm text-destructive">
              {getErrorMessage(contentQuery.error)}
            </div>
          ) : null}

          {contentQuery.data?.isBinary ? (
            <div className="flex h-full min-h-48 flex-col items-center justify-center gap-2 px-4 text-center text-sm text-muted-foreground">
              <AlertTriangle className="h-5 w-5 text-warning-solid" />
              <p>Arquivo binario. Visualizacao de texto indisponivel.</p>
            </div>
          ) : null}

          {contentQuery.data && !contentQuery.data.isBinary && showMarkdownPreview ? (
            <div className="h-full overflow-y-auto p-4 sm:p-6">
              <div className="linked-markdown mx-auto max-w-3xl">
                <ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]}>
                  {contentQuery.data.content}
                </ReactMarkdown>
              </div>
            </div>
          ) : null}

          {contentQuery.data && !contentQuery.data.isBinary && !showMarkdownPreview ? (
            <Suspense
              fallback={
                <div className="flex h-full min-h-48 items-center justify-center gap-2 text-sm text-muted-foreground">
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Preparando editor
                </div>
              }
            >
              <MonacoEditor
                value={contentQuery.data.content}
                language={language}
                theme={resolvedTheme === 'dark' ? 'vs-dark' : 'vs'}
                options={{
                  readOnly: true,
                  minimap: { enabled: false },
                  scrollBeyondLastLine: false,
                  fontFamily: 'JetBrains Mono Variable, ui-monospace, monospace',
                  fontSize: 13,
                  lineNumbers: 'on',
                  wordWrap: 'on',
                  automaticLayout: true,
                }}
                height="100%"
              />
            </Suspense>
          ) : null}
        </div>

        {contentQuery.data?.truncated ? (
          <div className="border-t border-border bg-warning-soft px-3 py-2 text-xs text-warning-foreground">
            Arquivo truncado para visualizacao. Abra no editor local para ver o conteudo completo.
          </div>
        ) : null}
      </div>
    </section>
  )
}
