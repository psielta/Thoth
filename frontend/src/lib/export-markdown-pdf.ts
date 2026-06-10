import { createElement } from 'react'
import { createRoot } from 'react-dom/client'
import { flushSync } from 'react-dom'
import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'
import remarkGfm from 'remark-gfm'

export type ExportMarkdownPdfOptions = {
  title: string
  subtitle?: string
  markdown: string
  filename?: string
}

const PDF_MARKDOWN_STYLES = `
  .pdf-export-root {
    position: fixed;
    left: -10000px;
    top: 0;
    width: 794px;
    box-sizing: border-box;
    background: #ffffff;
    color: #111111;
    font-family: system-ui, -apple-system, "Segoe UI", sans-serif;
    font-size: 11pt;
    line-height: 1.5;
    padding: 32px;
  }

  .pdf-export-header {
    margin-bottom: 1.5rem;
    padding-bottom: 0.75rem;
    border-bottom: 1px solid #d4d4d8;
  }

  .pdf-export-title {
    margin: 0;
    font-size: 1.35rem;
    font-weight: 700;
    line-height: 1.25;
    color: #111111;
  }

  .pdf-export-subtitle {
    margin: 0.35rem 0 0;
    font-size: 0.85rem;
    color: #52525b;
  }

  .pdf-export-body > * + * {
    margin-top: 0.85rem;
  }

  .pdf-export-body h1 {
    font-size: 1.55rem;
    font-weight: 750;
    line-height: 1.2;
  }

  .pdf-export-body h2 {
    border-bottom: 1px solid #d4d4d8;
    font-size: 1.2rem;
    font-weight: 720;
    line-height: 1.25;
    padding-bottom: 0.35rem;
  }

  .pdf-export-body h3 {
    font-size: 1rem;
    font-weight: 700;
    line-height: 1.3;
  }

  .pdf-export-body ul,
  .pdf-export-body ol {
    padding-left: 1.4rem;
  }

  .pdf-export-body ul {
    list-style: disc;
  }

  .pdf-export-body ol {
    list-style: decimal;
  }

  .pdf-export-body li + li {
    margin-top: 0.3rem;
  }

  .pdf-export-body a {
    color: #1d4ed8;
    font-weight: 600;
    text-decoration: underline;
  }

  .pdf-export-body blockquote {
    border-left: 3px solid #71717a;
    color: #52525b;
    padding-left: 0.85rem;
  }

  .pdf-export-body code {
    border-radius: 0.25rem;
    background: #f4f4f5;
    color: #111111;
    font-family: "JetBrains Mono", Consolas, monospace;
    font-size: 0.88em;
    padding: 0.1rem 0.3rem;
  }

  .pdf-export-body pre {
    max-width: 100%;
    overflow-x: auto;
    border-radius: 0.5rem;
    background: #18181b;
    color: #f4f4f5;
    font-family: "JetBrains Mono", Consolas, monospace;
    padding: 0.85rem 1rem;
    white-space: pre-wrap;
    word-break: break-word;
  }

  .pdf-export-body pre code {
    background: transparent;
    color: inherit;
    padding: 0;
  }

  .pdf-export-body table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.9rem;
  }

  .pdf-export-body th,
  .pdf-export-body td {
    border: 1px solid #d4d4d8;
    padding: 0.45rem 0.6rem;
    text-align: left;
  }

  .pdf-export-body th {
    background: #f4f4f5;
    font-weight: 700;
  }
`

export function sanitizePdfFilename(title: string): string {
  const normalized = title
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80)

  return normalized || 'documento'
}

function waitForNextFrame(): Promise<void> {
  return new Promise((resolve) => {
    requestAnimationFrame(() => {
      requestAnimationFrame(() => resolve())
    })
  })
}

function buildExportContainer(title: string, subtitle?: string): HTMLDivElement {
  const container = document.createElement('div')
  container.className = 'pdf-export-root'
  container.innerHTML = `
    <style>${PDF_MARKDOWN_STYLES}</style>
    <header class="pdf-export-header">
      <h1 class="pdf-export-title"></h1>
      ${subtitle ? '<p class="pdf-export-subtitle"></p>' : ''}
    </header>
    <div class="pdf-export-body"></div>
  `

  const titleElement = container.querySelector('.pdf-export-title')
  if (titleElement) {
    titleElement.textContent = title
  }

  if (subtitle) {
    const subtitleElement = container.querySelector('.pdf-export-subtitle')
    if (subtitleElement) {
      subtitleElement.textContent = subtitle
    }
  }

  return container
}

export async function exportMarkdownPdf({
  title,
  subtitle,
  markdown,
  filename,
}: ExportMarkdownPdfOptions): Promise<void> {
  const container = buildExportContainer(title, subtitle)
  const body = container.querySelector('.pdf-export-body')

  if (!body) {
    throw new Error('Nao foi possivel preparar o conteudo para exportacao.')
  }

  document.body.appendChild(container)

  const root = createRoot(body)

  try {
    flushSync(() => {
      root.render(
        createElement(ReactMarkdown, {
          remarkPlugins: [remarkGfm],
          rehypePlugins: [rehypeSanitize],
          children: markdown,
        }),
      )
    })

    await waitForNextFrame()

    const html2pdf = (await import('html2pdf.js')).default
    const outputName = `${filename ?? sanitizePdfFilename(title)}.pdf`

    await html2pdf()
      .set({
        margin: [15, 15, 15, 15],
        filename: outputName,
        image: { type: 'jpeg', quality: 0.98 },
        html2canvas: { scale: 2, useCORS: true, backgroundColor: '#ffffff' },
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' },
      })
      .from(container)
      .save()
  } finally {
    root.unmount()
    container.remove()
  }
}