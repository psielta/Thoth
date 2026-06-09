import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { generateMermaidDiagram, getAiModels, getAiSettings } from '@/api/ai'
import { GenerateMermaidDialog } from './generate-mermaid-dialog'

vi.mock('@/api/ai', () => ({
  getAiModels: vi.fn(),
  getAiSettings: vi.fn(),
  generateMermaidDiagram: vi.fn(),
}))

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}))

const workingDirectoryId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c0'
const diagramId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c1'

function renderDialog(overrides: Partial<Parameters<typeof GenerateMermaidDialog>[0]> = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  const props = {
    workingDirectoryId,
    diagramId,
    currentCode: '',
    onApply: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  }
  render(
    <QueryClientProvider client={queryClient}>
      <GenerateMermaidDialog {...props} />
    </QueryClientProvider>,
  )
  return props
}

describe('GenerateMermaidDialog', () => {
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
    vi.mocked(generateMermaidDiagram).mockResolvedValue({
      mermaidCode: 'sequenceDiagram\n  A->>B: hi',
      titleSuggestion: null,
      promptTokens: 9,
      candidateTokens: 6,
      warnings: [],
    })
  })

  afterEach(() => {
    cleanup()
  })

  it('sends instruction and diagram kind then applies the generated code', async () => {
    const user = userEvent.setup()
    const props = renderDialog()

    await user.type(screen.getByLabelText('Instrucao'), 'fluxo de login')
    await user.selectOptions(screen.getByLabelText('Tipo de diagrama'), 'sequence')
    await user.click(screen.getByRole('button', { name: /^Gerar$/ }))

    await waitFor(() => {
      expect(generateMermaidDiagram).toHaveBeenCalledWith(
        expect.objectContaining({
          instruction: 'fluxo de login',
          diagramKind: 'sequence',
          workingDirectoryId,
          diagramId,
        }),
        expect.anything(),
      )
    })

    await user.click(await screen.findByRole('button', { name: /Aplicar no editor/ }))
    expect(props.onApply).toHaveBeenCalledWith('sequenceDiagram\n  A->>B: hi')
  })
})
