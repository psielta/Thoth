import { afterEach, describe, expect, it, vi } from 'vitest'
import { exportMarkdownPdf, sanitizePdfFilename } from './export-markdown-pdf'

const saveMock = vi.fn().mockResolvedValue(undefined)
const fromMock = vi.fn().mockReturnValue({ save: saveMock })
const setMock = vi.fn().mockReturnValue({ from: fromMock })
const html2pdfMock = vi.fn().mockReturnValue({ set: setMock })

vi.mock('html2pdf.js', () => ({
  default: html2pdfMock,
}))

describe('sanitizePdfFilename', () => {
  it('normalizes accents and spaces', () => {
    expect(sanitizePdfFilename('Planejar Refatoração do Módulo X')).toBe(
      'planejar-refatoracao-do-modulo-x',
    )
  })

  it('falls back when title is empty', () => {
    expect(sanitizePdfFilename('   ')).toBe('documento')
  })
})

describe('exportMarkdownPdf', () => {
  afterEach(() => {
    document.body.innerHTML = ''
    vi.clearAllMocks()
  })

  it('renders markdown off-screen and triggers html2pdf save', async () => {
    await exportMarkdownPdf({
      title: 'Meu Prompt',
      subtitle: 'TASK-12',
      markdown: '# Titulo\n\nConteudo',
      filename: 'meu-prompt',
    })

    expect(html2pdfMock).toHaveBeenCalledTimes(1)
    expect(setMock).toHaveBeenCalledWith(
      expect.objectContaining({
        filename: 'meu-prompt.pdf',
        jsPDF: expect.objectContaining({ format: 'a4' }),
      }),
    )
    expect(fromMock).toHaveBeenCalledWith(expect.any(HTMLElement))
    expect(saveMock).toHaveBeenCalledTimes(1)
    expect(document.body.querySelector('.pdf-export-root')).toBeNull()
  })
})