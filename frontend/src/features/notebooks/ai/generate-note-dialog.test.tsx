import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { generateNoteMarkdown, getAiModels, getAiSettings } from '@/api/ai'
import { GenerateNoteDialog } from './generate-note-dialog'

vi.mock('@/api/ai', () => ({
  getAiModels: vi.fn(),
  getAiSettings: vi.fn(),
  generateNoteMarkdown: vi.fn(),
}))

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

const notebookId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c0'
const workingDirectoryId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c1'

function renderDialog(overrides: Partial<Parameters<typeof GenerateNoteDialog>[0]> = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  const props = {
    notebookId,
    workingDirectoryId,
    currentContent: '',
    onInsert: vi.fn(),
    onReplace: vi.fn(),
    onCreate: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  }
  render(
    <QueryClientProvider client={queryClient}>
      <GenerateNoteDialog {...props} />
    </QueryClientProvider>,
  )
  return props
}

describe('GenerateNoteDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(getAiModels).mockResolvedValue([
      {
        id: 'gemini-test',
        label: 'Gemini Test',
        thinkingMode: 'none',
        canDisableThinking: true,
        thinkingBudgetMin: 0,
        thinkingBudgetMax: 0,
        minCacheTokens: 1024,
      },
    ])
    vi.mocked(getAiSettings).mockResolvedValue({
      model: 'gemini-test',
      temperature: 0.4,
      thinkingEnabled: false,
      thinkingBudget: null,
      thinkingLevel: null,
    })
    vi.mocked(generateNoteMarkdown).mockResolvedValue({
      suggestedTitle: 'Titulo IA',
      contentMarkdown: 'Corpo gerado pela IA',
      promptTokens: 10,
      candidateTokens: 4,
    })
  })

  afterEach(() => {
    cleanup()
  })

  it('sends instruction and selected format then creates a new note', async () => {
    const user = userEvent.setup()
    const props = renderDialog()

    await user.type(screen.getByLabelText('Instrucao'), 'crie uma ata da reuniao')
    await user.selectOptions(screen.getByLabelText('Formato'), 'ata')
    await user.click(screen.getByRole('button', { name: /^Gerar$/ }))

    await waitFor(() => {
      expect(generateNoteMarkdown).toHaveBeenCalledWith(
        expect.objectContaining({
          instruction: 'crie uma ata da reuniao',
          format: 'ata',
          notebookId,
        }),
        expect.anything(),
      )
    })

    await user.click(await screen.findByRole('button', { name: /Criar nova nota/ }))
    expect(props.onCreate).toHaveBeenCalledWith(
      expect.objectContaining({ contentMarkdown: 'Corpo gerado pela IA', suggestedTitle: 'Titulo IA' }),
    )
  })

  it('applies the result with replace and insert', async () => {
    const user = userEvent.setup()
    const props = renderDialog()

    await user.type(screen.getByLabelText('Instrucao'), 'gere algo')
    await user.click(screen.getByRole('button', { name: /^Gerar$/ }))

    await user.click(await screen.findByRole('button', { name: /Substituir/ }))
    expect(props.onReplace).toHaveBeenCalledWith(
      expect.objectContaining({ contentMarkdown: 'Corpo gerado pela IA' }),
    )
  })
})
