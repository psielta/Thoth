import remarkGfm from 'remark-gfm'
import remarkParse from 'remark-parse'
import { unified } from 'unified'

export type ExportMarkdownPdfOptions = {
  title: string
  subtitle?: string
  markdown: string
  filename?: string
}

type MarkdownNode = {
  type: string
  value?: string
  depth?: number
  ordered?: boolean
  start?: number
  checked?: boolean | null
  url?: string
  alt?: string
  children?: MarkdownNode[]
}

type JsPdfConstructor = typeof import('jspdf').jsPDF
type PdfDocument = InstanceType<JsPdfConstructor>
type FontStyle = 'normal' | 'bold' | 'italic' | 'bolditalic'
type Rgb = [number, number, number]

type TextOptions = {
  x?: number
  maxWidth?: number
  fontSize?: number
  fontStyle?: FontStyle
  color?: Rgb
  lineHeight?: number
}

const PAGE = {
  marginTop: 18,
  marginRight: 16,
  marginBottom: 18,
  marginLeft: 16,
}

const COLORS = {
  text: [17, 17, 17] as Rgb,
  muted: [82, 82, 91] as Rgb,
  border: [212, 212, 216] as Rgb,
  soft: [244, 244, 245] as Rgb,
  link: [29, 78, 216] as Rgb,
}

const FONT = {
  body: 10.5,
  small: 8.5,
  code: 8.5,
  h1: 17,
  h2: 14,
  h3: 12,
  title: 16,
  subtitle: 9,
}

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

function createMarkdownTree(markdown: string): MarkdownNode {
  return unified().use(remarkParse).use(remarkGfm).parse(markdown) as MarkdownNode
}

function cleanParagraphText(text: string): string {
  return text
    .replace(/\r\n?/g, '\n')
    .split('\n')
    .map((line) => line.replace(/\s+/g, ' ').trim())
    .filter(Boolean)
    .join('\n')
}

function normalizeInlineText(text: string): string {
  return text.replace(/\s+/g, ' ')
}

function inlineText(node: MarkdownNode): string {
  switch (node.type) {
    case 'text':
    case 'inlineCode':
      return node.value ?? ''
    case 'break':
      return '\n'
    case 'link': {
      const label = childrenText(node)
      if (!node.url || node.url === label) {
        return label
      }

      return `${label} (${node.url})`
    }
    case 'image':
      return node.alt ? `[Imagem: ${node.alt}]` : '[Imagem]'
    default:
      return childrenText(node)
  }
}

function childrenText(node: MarkdownNode): string {
  return (node.children ?? []).map(inlineText).join('')
}

function blockText(node: MarkdownNode): string {
  if (node.type === 'code') {
    return node.value ?? ''
  }

  if (node.type === 'table') {
    return (node.children ?? [])
      .map((row) => (row.children ?? []).map((cell) => cleanParagraphText(childrenText(cell))).join(' | '))
      .join('\n')
  }

  return cleanParagraphText(childrenText(node))
}

function listItemText(node: MarkdownNode): string {
  return (node.children ?? [])
    .filter((child) => child.type !== 'list')
    .map(blockText)
    .filter(Boolean)
    .join('\n')
}

class MarkdownPdfRenderer {
  private readonly doc: PdfDocument
  private readonly pageWidth: number
  private readonly pageHeight: number
  private y: number

  constructor(doc: PdfDocument) {
    this.doc = doc
    this.pageWidth = doc.internal.pageSize.getWidth()
    this.pageHeight = doc.internal.pageSize.getHeight()
    this.y = PAGE.marginTop
  }

  render(title: string, subtitle: string | undefined, markdown: string): void {
    this.renderHeader(title, subtitle)

    const tree = createMarkdownTree(markdown)
    const blocks = tree.children ?? []

    if (blocks.length === 0) {
      this.writeWrappedText('Sem conteudo.', {
        fontSize: FONT.body,
        color: COLORS.muted,
      })
    } else {
      this.renderBlocks(blocks)
    }

    this.renderPageNumbers()
  }

  private get contentWidth(): number {
    return this.pageWidth - PAGE.marginLeft - PAGE.marginRight
  }

  private renderHeader(title: string, subtitle?: string): void {
    this.writeWrappedText(title, {
      fontSize: FONT.title,
      fontStyle: 'bold',
      lineHeight: 7,
    })

    if (subtitle) {
      this.y += 1
      this.writeWrappedText(subtitle, {
        fontSize: FONT.subtitle,
        color: COLORS.muted,
        lineHeight: 4.2,
      })
    }

    this.y += 4
    this.ensureSpace(2)
    this.doc.setDrawColor(...COLORS.border)
    this.doc.line(PAGE.marginLeft, this.y, this.pageWidth - PAGE.marginRight, this.y)
    this.y += 8
  }

  private renderBlocks(blocks: MarkdownNode[], indent = 0): void {
    for (const block of blocks) {
      this.renderBlock(block, indent)
    }
  }

  private renderBlock(block: MarkdownNode, indent = 0): void {
    switch (block.type) {
      case 'heading':
        this.renderHeading(block, indent)
        break
      case 'paragraph':
        this.renderParagraph(block, indent)
        break
      case 'list':
        this.renderList(block, indent)
        break
      case 'code':
        this.renderCode(block, indent)
        break
      case 'blockquote':
        this.renderQuote(block, indent)
        break
      case 'thematicBreak':
        this.renderRule(indent)
        break
      case 'table':
        this.renderTable(block, indent)
        break
      default:
        this.renderFallback(block, indent)
        break
    }
  }

  private renderHeading(block: MarkdownNode, indent: number): void {
    const text = cleanParagraphText(childrenText(block))
    if (!text) {
      return
    }

    const depth = block.depth ?? 3
    const fontSize = depth === 1 ? FONT.h1 : depth === 2 ? FONT.h2 : FONT.h3
    const lineHeight = depth === 1 ? 7.4 : depth === 2 ? 6.3 : 5.4

    this.y += depth === 1 ? 3 : 2
    this.writeWrappedText(text, {
      x: PAGE.marginLeft + indent,
      maxWidth: this.contentWidth - indent,
      fontSize,
      fontStyle: 'bold',
      lineHeight,
    })

    if (depth === 2) {
      this.y += 1
      this.ensureSpace(2)
      this.doc.setDrawColor(...COLORS.border)
      this.doc.line(PAGE.marginLeft + indent, this.y, this.pageWidth - PAGE.marginRight, this.y)
      this.y += 4
    } else {
      this.y += 3
    }
  }

  private renderParagraph(block: MarkdownNode, indent: number): void {
    const text = cleanParagraphText(childrenText(block))
    if (!text) {
      return
    }

    this.writeWrappedText(text, {
      x: PAGE.marginLeft + indent,
      maxWidth: this.contentWidth - indent,
      fontSize: FONT.body,
      lineHeight: 5.2,
    })
    this.y += 3
  }

  private renderList(block: MarkdownNode, indent: number): void {
    const items = block.children ?? []
    const ordered = block.ordered === true
    const start = block.start ?? 1

    this.y += 1

    items.forEach((item, index) => {
      const marker = ordered ? `${start + index}.` : '-'
      const taskPrefix = item.checked === true ? '[x] ' : item.checked === false ? '[ ] ' : ''
      const text = `${marker} ${taskPrefix}${listItemText(item)}`.trim()

      if (text) {
        this.writeWrappedText(text, {
          x: PAGE.marginLeft + indent,
          maxWidth: this.contentWidth - indent,
          fontSize: FONT.body,
          lineHeight: 5.1,
        })
      }

      for (const child of item.children ?? []) {
        if (child.type === 'list') {
          this.renderList(child, indent + 7)
        }
      }

      this.y += 1.5
    })

    this.y += 2
  }

  private renderCode(block: MarkdownNode, indent: number): void {
    const code = block.value ?? ''
    if (!code) {
      return
    }

    const x = PAGE.marginLeft + indent
    const maxWidth = this.contentWidth - indent
    const lines = code
      .replace(/\r\n?/g, '\n')
      .split('\n')
      .flatMap((line) => this.splitText(line || ' ', maxWidth - 5, FONT.code, 'normal', 'courier'))

    this.y += 2

    for (const line of lines) {
      this.ensureSpace(5)
      this.doc.setFillColor(...COLORS.soft)
      this.doc.rect(x, this.y - 3.5, maxWidth, 5, 'F')
      this.setTextStyle(FONT.code, 'normal', COLORS.text, 'courier')
      this.doc.text(line, x + 2.5, this.y)
      this.y += 4.4
    }

    this.y += 4
  }

  private renderQuote(block: MarkdownNode, indent: number): void {
    const text = cleanParagraphText((block.children ?? []).map(blockText).filter(Boolean).join('\n'))
    if (!text) {
      return
    }

    const x = PAGE.marginLeft + indent
    const lineX = x
    const textX = x + 4
    const maxWidth = this.contentWidth - indent - 4
    const lines = this.splitText(text, maxWidth, FONT.body, 'italic')

    this.y += 1
    for (const line of lines) {
      this.ensureSpace(5.2)
      this.doc.setDrawColor(...COLORS.muted)
      this.doc.line(lineX, this.y - 3.7, lineX, this.y + 1.2)
      this.setTextStyle(FONT.body, 'italic', COLORS.muted)
      this.doc.text(line, textX, this.y)
      this.y += 5.2
    }

    this.y += 4
  }

  private renderRule(indent: number): void {
    this.y += 2
    this.ensureSpace(2)
    this.doc.setDrawColor(...COLORS.border)
    this.doc.line(PAGE.marginLeft + indent, this.y, this.pageWidth - PAGE.marginRight, this.y)
    this.y += 6
  }

  private renderTable(block: MarkdownNode, indent: number): void {
    const rows = blockText(block)
    if (!rows) {
      return
    }

    this.writeWrappedText(rows, {
      x: PAGE.marginLeft + indent,
      maxWidth: this.contentWidth - indent,
      fontSize: FONT.code,
      fontStyle: 'normal',
      lineHeight: 4.5,
    })
    this.y += 4
  }

  private renderFallback(block: MarkdownNode, indent: number): void {
    const text = blockText(block)
    if (!text) {
      return
    }

    this.writeWrappedText(text, {
      x: PAGE.marginLeft + indent,
      maxWidth: this.contentWidth - indent,
      fontSize: FONT.body,
      lineHeight: 5.2,
    })
    this.y += 3
  }

  private writeWrappedText(text: string, options: TextOptions = {}): void {
    const x = options.x ?? PAGE.marginLeft
    const maxWidth = options.maxWidth ?? this.contentWidth
    const fontSize = options.fontSize ?? FONT.body
    const fontStyle = options.fontStyle ?? 'normal'
    const color = options.color ?? COLORS.text
    const lineHeight = options.lineHeight ?? 5
    const lines = this.splitText(text, maxWidth, fontSize, fontStyle)

    this.setTextStyle(fontSize, fontStyle, color)

    for (const line of lines) {
      this.ensureSpace(lineHeight)
      this.doc.text(line, x, this.y)
      this.y += lineHeight
    }
  }

  private splitText(
    text: string,
    maxWidth: number,
    fontSize: number,
    fontStyle: FontStyle,
    fontFamily = 'helvetica',
  ): string[] {
    this.setTextStyle(fontSize, fontStyle, COLORS.text, fontFamily)

    return text.split('\n').flatMap((line) => {
      const normalized = normalizeInlineText(line)
      const wrapped = this.doc.splitTextToSize(normalized || ' ', maxWidth) as string[]
      return wrapped.length > 0 ? wrapped : [' ']
    })
  }

  private ensureSpace(requiredHeight: number): void {
    if (this.y + requiredHeight <= this.pageHeight - PAGE.marginBottom) {
      return
    }

    this.doc.addPage('a4', 'portrait')
    this.y = PAGE.marginTop
  }

  private setTextStyle(
    fontSize: number,
    fontStyle: FontStyle,
    color: Rgb,
    fontFamily = 'helvetica',
  ): void {
    this.doc.setFont(fontFamily, fontStyle)
    this.doc.setFontSize(fontSize)
    this.doc.setTextColor(...color)
  }

  private renderPageNumbers(): void {
    const pageCount = this.doc.getNumberOfPages()

    for (let page = 1; page <= pageCount; page += 1) {
      this.doc.setPage(page)
      this.setTextStyle(FONT.small, 'normal', COLORS.muted)
      this.doc.text(
        `${page}/${pageCount}`,
        this.pageWidth - PAGE.marginRight,
        this.pageHeight - 8,
        { align: 'right' },
      )
    }
  }
}

export async function exportMarkdownPdf({
  title,
  subtitle,
  markdown,
  filename,
}: ExportMarkdownPdfOptions): Promise<void> {
  const outputName = `${filename ?? sanitizePdfFilename(title)}.pdf`
  const { jsPDF } = await import('jspdf')
  const doc = new jsPDF({ unit: 'mm', format: 'a4', orientation: 'portrait' })

  doc.setProperties({ title })

  const renderer = new MarkdownPdfRenderer(doc)
  renderer.render(title, subtitle, markdown)
  doc.save(outputName)
}
