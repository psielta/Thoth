import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Clock, FileText, GitBranch, MessageSquareText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LinkedDocumentsPanel } from '@/features/linked-documents/linked-documents-panel'
import { PromptChildrenPanel } from '@/features/prompts/prompt-children-panel'
import { PromptForm } from '@/features/prompts/prompt-form'
import { PromptVersions } from '@/features/prompts/prompt-versions'
import { WorkflowPanel } from '@/features/workflow/workflow-panel'

type DetailTab = 'prompt' | 'linked-plan' | 'children' | 'timeline'

const TABS: ReadonlyArray<DetailTab> = ['prompt', 'linked-plan', 'children', 'timeline']

function isDetailTab(value: unknown): value is DetailTab {
  return typeof value === 'string' && (TABS as readonly string[]).includes(value)
}

export const Route = createFileRoute('/workspaces/$workspaceId/prompts/$promptId')({
  validateSearch: (search: Record<string, unknown>): { tab?: DetailTab } => ({
    tab: isDetailTab(search.tab) ? search.tab : undefined,
  }),
  component: PromptDetailPage,
})

function PromptDetailPage() {
  const { workspaceId, promptId } = Route.useParams()
  const { tab } = Route.useSearch()
  const navigate = useNavigate()
  const activeTab = tab ?? 'prompt'

  const setActiveTab = (nextTab: DetailTab) => {
    void navigate({
      to: '/workspaces/$workspaceId/prompts/$promptId',
      params: { workspaceId, promptId },
      search: nextTab === 'prompt' ? {} : { tab: nextTab },
      replace: true,
    })
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap gap-2 rounded-lg border border-border bg-card p-2">
        <Button
          type="button"
          variant={activeTab === 'prompt' ? 'default' : 'ghost'}
          size="sm"
          aria-pressed={activeTab === 'prompt'}
          onClick={() => setActiveTab('prompt')}
        >
          <MessageSquareText className="h-4 w-4" />
          Prompt
        </Button>
        <Button
          type="button"
          variant={activeTab === 'timeline' ? 'default' : 'ghost'}
          size="sm"
          aria-pressed={activeTab === 'timeline'}
          onClick={() => setActiveTab('timeline')}
        >
          <Clock className="h-4 w-4" />
          Timeline
        </Button>
        <Button
          type="button"
          variant={activeTab === 'linked-plan' ? 'default' : 'ghost'}
          size="sm"
          aria-pressed={activeTab === 'linked-plan'}
          onClick={() => setActiveTab('linked-plan')}
        >
          <FileText className="h-4 w-4" />
          Plano vinculado
        </Button>
        <Button
          type="button"
          variant={activeTab === 'children' ? 'default' : 'ghost'}
          size="sm"
          aria-pressed={activeTab === 'children'}
          onClick={() => setActiveTab('children')}
        >
          <GitBranch className="h-4 w-4" />
          Prompts filhos
        </Button>
      </div>

      {activeTab === 'prompt' ? (
        <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_18rem]">
          <PromptForm workingDirectoryId={workspaceId} promptId={promptId} />
          <PromptVersions promptId={promptId} />
        </div>
      ) : null}

      {activeTab === 'timeline' ? <WorkflowPanel promptId={promptId} onNavigateTab={setActiveTab} /> : null}

      {activeTab === 'linked-plan' ? <LinkedDocumentsPanel promptId={promptId} /> : null}

      {activeTab === 'children' ? (
        <PromptChildrenPanel workingDirectoryId={workspaceId} parentPromptId={promptId} />
      ) : null}
    </div>
  )
}
