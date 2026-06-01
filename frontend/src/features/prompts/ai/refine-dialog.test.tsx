import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getAiModels, getAiSettings, refinePrompt } from '@/api/ai'
import { searchFiles, validateFileReferences } from '@/api/files'
import { RefineDialog } from './refine-dialog'

vi.mock('@/api/ai', () => ({
  getAiModels: vi.fn(),
  getAiSettings: vi.fn(),
  refinePrompt: vi.fn(),
}))

vi.mock('@/api/files', () => ({
  searchFiles: vi.fn(),
  validateFileReferences: vi.fn(),
}))

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

vi.mock('../prompt-editor', () => ({
  PromptEditor: ({
    value,
    onChange,
  }: {
    value: string
    onChange: (value: string, mentions: unknown[]) => void
  }) => (
    <textarea
      aria-label="Instruções de refinamento"
      value={value}
      onChange={(event) => onChange(event.currentTarget.value, [])}
    />
  ),
}))

const workingDirectoryId = '019e9f6a-9fb2-7f24-ac3a-bf099d2c93c0'

function renderDialog() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <RefineDialog
        content="Prompt original"
        workingDirectoryId={workingDirectoryId}
        onApply={vi.fn()}
        onClose={vi.fn()}
      />
    </QueryClientProvider>,
  )
}

describe('RefineDialog', () => {
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
    vi.mocked(refinePrompt).mockResolvedValue({
      content: 'Prompt refinado',
      promptTokens: 12,
      candidateTokens: 5,
    })
    vi.mocked(searchFiles).mockResolvedValue([
      {
        relativePath: 'src/main.cs',
        fileName: 'main.cs',
        isDirectory: false,
        score: 100,
      },
      {
        relativePath: 'src',
        fileName: 'src',
        isDirectory: true,
        score: 90,
      },
    ])
    vi.mocked(validateFileReferences).mockResolvedValue([])
  })

  afterEach(() => {
    cleanup()
  })

  it('sends selected context files and custom instructions to refine', async () => {
    const user = userEvent.setup()
    renderDialog()

    await user.type(screen.getByLabelText('Buscar arquivos de contexto'), 'main')
    await user.click(await screen.findByRole('button', { name: /src\/main\.cs/ }))
    expect(screen.getByLabelText('Arquivos selecionados')).toHaveTextContent('src/main.cs')

    await user.type(screen.getByLabelText('Instruções de refinamento'), 'Torne o tom mais formal')
    await user.click(screen.getByRole('button', { name: /^Refinar$/ }))

    await waitFor(() => {
      expect(refinePrompt).toHaveBeenCalledWith(expect.objectContaining({
        content: 'Prompt original',
        workingDirectoryId,
        contextFiles: ['src/main.cs'],
        customInstructions: 'Torne o tom mais formal',
      }))
    })
    expect(await screen.findByText('Prompt refinado')).toBeInTheDocument()
  })
})
