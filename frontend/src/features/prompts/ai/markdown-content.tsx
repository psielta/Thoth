import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { normalizeMarkdownTableBlocks } from '@/lib/markdown-tables'

export function MarkdownContent({ content }: { content: string }) {
  return (
    <div className="markdown-chat text-sm text-foreground">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          p({ children }) {
            return <p className="mb-3 last:mb-0 leading-relaxed">{children}</p>
          },
          ul({ children }) {
            return <ul className="mb-3 list-disc space-y-1 pl-5 last:mb-0">{children}</ul>
          },
          ol({ children }) {
            return <ol className="mb-3 list-decimal space-y-1 pl-5 last:mb-0">{children}</ol>
          },
          li({ children }) {
            return <li className="leading-relaxed">{children}</li>
          },
          h1({ children }) {
            return <h1 className="mb-3 mt-4 text-base font-bold text-foreground first:mt-0">{children}</h1>
          },
          h2({ children }) {
            return <h2 className="mb-2 mt-4 text-sm font-bold text-foreground first:mt-0">{children}</h2>
          },
          h3({ children }) {
            return <h3 className="mb-2 mt-3 text-sm font-semibold text-foreground first:mt-0">{children}</h3>
          },
          blockquote({ children }) {
            return (
              <blockquote className="my-3 border-l-[3px] border-primary pl-3 italic text-muted-foreground">
                {children}
              </blockquote>
            )
          },
          pre({ children }) {
            return (
              <pre className="my-3 max-w-full overflow-x-auto rounded-lg bg-code p-4 text-xs leading-relaxed">
                {children}
              </pre>
            )
          },
          code({ className, children }) {
            if (className) {
              return <code className="font-mono text-code-foreground">{children}</code>
            }
            return (
              <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs text-success-foreground">
                {children}
              </code>
            )
          },
          table({ children }) {
            return (
              <div className="my-3 overflow-x-auto rounded-lg border border-secondary">
                <table className="w-full text-xs">{children}</table>
              </div>
            )
          },
          thead({ children }) {
            return <thead className="bg-background">{children}</thead>
          },
          th({ children }) {
            return (
              <th className="border-b border-secondary px-3 py-2 text-left font-semibold text-foreground">
                {children}
              </th>
            )
          },
          td({ children }) {
            return (
              <td className="border-b border-secondary px-3 py-2 text-foreground last:border-b-0">
                {children}
              </td>
            )
          },
          a({ children, href }) {
            return (
              <a
                href={href}
                className="text-primary underline underline-offset-2 hover:text-foreground"
                target="_blank"
                rel="noopener noreferrer"
              >
                {children}
              </a>
            )
          },
          strong({ children }) {
            return <strong className="font-semibold text-foreground">{children}</strong>
          },
          em({ children }) {
            return <em className="italic">{children}</em>
          },
          hr() {
            return <hr className="my-4 border-secondary" />
          },
        }}
      >
        {normalizeMarkdownTableBlocks(content)}
      </ReactMarkdown>
    </div>
  )
}
