import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as filesApi from '@/api/files'
import { FileViewerPanel } from './file-viewer-panel'

vi.mock('@/api/files')
vi.mock('./use-file-subscription', () => ({
  useFileSubscription: () => {},
}))
vi.mock('@/components/theme/theme-provider', () => ({
  useTheme: () => ({ resolvedTheme: 'light' }),
}))
vi.mock('./monaco-setup', () => ({}))

type MonacoMockProps = {
  value: string
  options: { fontSize: number; minimap: { enabled: boolean }; wordWrap: string }
}

vi.mock('@monaco-editor/react', () => ({
  default: ({ value, options }: MonacoMockProps) => (
    <div
      data-testid="monaco-editor"
      data-font-size={String(options.fontSize)}
      data-minimap={String(options.minimap.enabled)}
      data-word-wrap={options.wordWrap}
    >
      {value}
    </div>
  ),
}))

function renderPanel(relativePath: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <FileViewerPanel workingDirectoryId="ws-1" relativePath={relativePath} />
    </QueryClientProvider>,
  )
}

describe('FileViewerPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    vi.mocked(filesApi.getFileContent).mockImplementation(async (_workingDirectoryId, relativePath) => ({
      relativePath,
      content: relativePath.endsWith('.md') ? '# Titulo\n\nParagrafo do plano.' : 'const total = 1',
      sizeBytes: 100,
      truncated: false,
      isBinary: false,
    }))
  })

  afterEach(() => {
    cleanup()
  })

  it('renders the Monaco viewer without the markdown toggle for non-markdown files', async () => {
    renderPanel('src/app.ts')

    expect(await screen.findByTestId('monaco-editor')).toHaveTextContent('const total = 1')
    expect(screen.queryByRole('group', { name: 'Modo de visualizacao do markdown' })).not.toBeInTheDocument()
  })

  it('toggles a markdown file between code and rendered preview', async () => {
    renderPanel('docs/plano.md')

    expect(await screen.findByTestId('monaco-editor')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Visual' }))
    expect(screen.getByRole('heading', { name: 'Titulo' })).toBeInTheDocument()
    expect(screen.queryByTestId('monaco-editor')).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Codigo' }))
    expect(await screen.findByTestId('monaco-editor')).toBeInTheDocument()
  })

  it('persists the markdown view preference between mounts', async () => {
    const first = renderPanel('docs/plano.md')
    await screen.findByTestId('monaco-editor')
    await userEvent.click(screen.getByRole('button', { name: 'Visual' }))
    first.unmount()

    renderPanel('docs/plano.md')

    expect(await screen.findByRole('heading', { name: 'Titulo' })).toBeInTheDocument()
    expect(screen.queryByTestId('monaco-editor')).not.toBeInTheDocument()
  })
})
