import { useEffect, useMemo, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'
import { normalizeMarkdownTableBlocks } from '@/lib/markdown-tables'
import { cn } from '@/lib/utils'

type OutlineEntry = {
  depth: number
  text: string
}

type MarkdownFilePreviewProps = {
  content: string
  showOutline: boolean
}

const HEADING_SELECTOR = 'h1, h2, h3, h4, h5, h6'

/**
 * Markdown renderizado do viewer de arquivos com sumario (document outline).
 * O outline e derivado dos headings ja renderizados no DOM — fonte unica de
 * verdade, sem reparsear o markdown — e o clique rola ate o heading de mesmo
 * indice dentro do container de scroll do preview.
 */
export function MarkdownFilePreview({ content, showOutline }: MarkdownFilePreviewProps) {
  const markdownRef = useRef<HTMLDivElement>(null)
  const [outline, setOutline] = useState<OutlineEntry[]>([])
  const renderedContent = useMemo(() => normalizeMarkdownTableBlocks(content), [content])

  useEffect(() => {
    const container = markdownRef.current
    if (!container) {
      return
    }

    const headings = Array.from(container.querySelectorAll(HEADING_SELECTOR))
    setOutline(
      headings.map((heading) => ({
        depth: Number.parseInt(heading.tagName.slice(1), 10),
        text: heading.textContent?.trim() ?? '',
      })),
    )
  }, [renderedContent])

  const scrollToHeading = (index: number) => {
    const heading = markdownRef.current?.querySelectorAll(HEADING_SELECTOR)[index]
    heading?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  const outlineVisible = showOutline && outline.length > 0

  return (
    <div className={cn('grid h-full min-h-0', outlineVisible && 'md:grid-cols-[14rem_minmax(0,1fr)]')}>
      {outlineVisible ? (
        <nav
          aria-label="Sumario do documento"
          className="hidden min-h-0 flex-col gap-1 overflow-y-auto border-r border-border p-3 md:flex"
        >
          <p className="shrink-0 px-1 text-[0.65rem] font-semibold uppercase tracking-wide text-muted-foreground">
            Sumario
          </p>
          {outline.map((entry, index) => (
            <button
              key={`${index}-${entry.text}`}
              type="button"
              onClick={() => scrollToHeading(index)}
              title={entry.text}
              className={cn(
                // shrink-0: dentro do flex-col com overflow, os itens encolhiam
                // em documentos com muitos headings e o truncate clipava o texto
                // a uma fatia de poucos pixels; sem shrink eles transbordam para
                // o scroll do painel.
                'shrink-0 truncate rounded px-1.5 py-1 text-left text-xs transition-colors hover:bg-secondary hover:text-foreground',
                entry.depth <= 1 ? 'font-semibold text-foreground' : 'text-muted-foreground',
              )}
              style={{ paddingLeft: `${0.375 + (entry.depth - 1) * 0.75}rem` }}
            >
              {entry.text}
            </button>
          ))}
        </nav>
      ) : null}

      <div className="h-full min-h-0 overflow-y-auto p-4 sm:p-6">
        <div ref={markdownRef} className="linked-markdown mx-auto max-w-3xl">
          <ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeSanitize]}>
            {renderedContent}
          </ReactMarkdown>
        </div>
      </div>
    </div>
  )
}
