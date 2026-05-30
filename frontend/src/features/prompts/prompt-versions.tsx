import { useQuery } from '@tanstack/react-query'
import { Clock3, Loader2 } from 'lucide-react'
import { listPromptVersions } from '@/api/prompts'
import { queryKeys } from '@/api/query-keys'

export function PromptVersions({ promptId }: { promptId: string }) {
  const versionsQuery = useQuery({
    queryKey: queryKeys.prompts.versions(promptId),
    queryFn: () => listPromptVersions(promptId),
  })

  return (
    <aside className="grid content-start gap-3 rounded-lg border border-[#d9dfd5] bg-white p-4">
      <div className="flex items-center gap-2 text-sm font-semibold text-[#172126]">
        <Clock3 className="h-4 w-4 text-[#5e7461]" />
        Versoes
      </div>

      {versionsQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-[#66746b]">
          <Loader2 className="h-4 w-4 animate-spin" />
          Carregando
        </div>
      ) : null}

      <div className="grid gap-2">
        {versionsQuery.data?.map((version) => (
          <div key={version.id} className="rounded-md border border-[#d9dfd5] p-3">
            <div className="text-sm font-medium text-[#172126]">v{version.versionNumber}</div>
            <div className="mt-1 text-xs text-[#66746b]">
              {new Intl.DateTimeFormat('pt-BR', {
                dateStyle: 'short',
                timeStyle: 'short',
              }).format(new Date(version.createdAtUtc))}
            </div>
          </div>
        ))}
      </div>
    </aside>
  )
}
