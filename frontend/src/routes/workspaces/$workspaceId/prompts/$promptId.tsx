import { createFileRoute } from '@tanstack/react-router'
import { FileText, GitBranch, MessageSquareText } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { LinkedDocumentsPanel } from '@/features/linked-documents/linked-documents-panel'
import { PromptChildrenPanel } from '@/features/prompts/prompt-children-panel'
import { PromptForm } from '@/features/prompts/prompt-form'
import { PromptVersions } from '@/features/prompts/prompt-versions'

export const Route = createFileRoute('/workspaces/$workspaceId/prompts/$promptId')({
  component: PromptDetailPage,
})

function PromptDetailPage() {
  const { workspaceId, promptId } = Route.useParams()
  const [activeTab, setActiveTab] = useState<'prompt' | 'linked-plan' | 'children'>('prompt')

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap gap-2 rounded-lg border border-[#d9dfd5] bg-white p-2">
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

      {activeTab === 'linked-plan' ? (
        <LinkedDocumentsPanel promptId={promptId} />
      ) : null}

      {activeTab === 'children' ? (
        <PromptChildrenPanel workingDirectoryId={workspaceId} parentPromptId={promptId} />
      ) : null}
    </div>
  )
}
