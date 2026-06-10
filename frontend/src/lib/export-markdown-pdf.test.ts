import { afterEach, describe, expect, it, vi } from 'vitest'
import { exportMarkdownPdf, sanitizePdfFilename } from './export-markdown-pdf'

const mocks = vi.hoisted(() => {
  const addPageMock = vi.fn()
  const lineMock = vi.fn()
  const rectMock = vi.fn()
  const saveMock = vi.fn()
  const setDrawColorMock = vi.fn()
  const setFillColorMock = vi.fn()
  const setFontMock = vi.fn()
  const setFontSizeMock = vi.fn()
  const setPageMock = vi.fn()
  const setPropertiesMock = vi.fn()
  const setTextColorMock = vi.fn()
  const splitTextToSizeMock = vi.fn((value: string) => String(value).split('\n'))
  const textMock = vi.fn()

  const jsPDFMock = vi.fn(function JsPdfConstructor() {
    return {
      addPage: addPageMock,
      getNumberOfPages: vi.fn(() => 1),
      internal: {
        pageSize: {
          getHeight: () => 297,
          getWidth: () => 210,
        },
      },
      line: lineMock,
      rect: rectMock,
      save: saveMock,
      setDrawColor: setDrawColorMock,
      setFillColor: setFillColorMock,
      setFont: setFontMock,
      setFontSize: setFontSizeMock,
      setPage: setPageMock,
      setProperties: setPropertiesMock,
      setTextColor: setTextColorMock,
      splitTextToSize: splitTextToSizeMock,
      text: textMock,
    }
  })

  return {
    jsPDFMock,
    saveMock,
    setPropertiesMock,
    textMock,
  }
})

vi.mock('jspdf', () => ({
  jsPDF: mocks.jsPDFMock,
}))

describe('sanitizePdfFilename', () => {
  it('normalizes accents and spaces', () => {
    expect(sanitizePdfFilename('Planejar Refatora\u00e7\u00e3o do M\u00f3dulo X')).toBe(
      'planejar-refatoracao-do-modulo-x',
    )
  })

  it('falls back when title is empty', () => {
    expect(sanitizePdfFilename('   ')).toBe('documento')
  })
})

describe('exportMarkdownPdf', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('renders markdown directly with jsPDF and saves the file', async () => {
    await exportMarkdownPdf({
      title: 'Meu Prompt',
      subtitle: 'TASK-12',
      markdown: '# Titulo\n\nConteudo\n\n- item',
      filename: 'meu-prompt',
    })

    expect(mocks.jsPDFMock).toHaveBeenCalledWith({ unit: 'mm', format: 'a4', orientation: 'portrait' })
    expect(mocks.setPropertiesMock).toHaveBeenCalledWith({ title: 'Meu Prompt' })
    expect(mocks.saveMock).toHaveBeenCalledWith('meu-prompt.pdf')

    const writtenText = mocks.textMock.mock.calls.map(([value]) => String(value))

    expect(writtenText).toContain('Meu Prompt')
    expect(writtenText).toContain('TASK-12')
    expect(writtenText).toContain('Titulo')
    expect(writtenText).toContain('Conteudo')
    expect(writtenText).toContain('- item')
  })
})
