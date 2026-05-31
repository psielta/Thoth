import { useMutation, useQuery } from '@tanstack/react-query'
import { Loader2, Sparkles, X } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { getAiSettings, refinePrompt } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'
import { getErrorMessage } from '@/api/client'
import { Button } from '@/components/ui/button'
import { AiModelConfig, type ModelConfig } from './ai-model-config'

type RefineDialogProps = {
  content: string
  onApply: (refined: string) => void
  onClose: () => void
}

export function RefineDialog({ content, onApply, onClose }: RefineDialogProps) {
  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [config, setConfig] = useState<ModelConfig>({
    model: settingsQuery.data?.model ?? 'gemini-2.5-flash',
    temperature: settingsQuery.data?.temperature ?? 0.7,
    thinkingEnabled: settingsQuery.data?.thinkingEnabled ?? false,
    thinkingBudget: settingsQuery.data?.thinkingBudget ?? null,
    thinkingLevel: settingsQuery.data?.thinkingLevel ?? null,
  })

  const [preview, setPreview] = useState<string | null>(null)

  const refineMutation = useMutation({
    mutationFn: () =>
      refinePrompt({
        content,
        model: config.model,
        temperature: config.temperature,
        thinkingMode: config.thinkingEnabled
          ? config.thinkingBudget != null
            ? 'budget'
            : config.thinkingLevel != null
              ? 'level'
              : 'none'
          : 'none',
        thinkingBudget: config.thinkingEnabled ? config.thinkingBudget : null,
        thinkingLevel: config.thinkingEnabled ? config.thinkingLevel : null,
      }),
    onSuccess: (result) => {
      setPreview(result.content)
      toast.success(`Prompt refinado. ${result.promptTokens} tokens de entrada, ${result.candidateTokens} gerados.`)
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  })

  const handleApply = () => {
    if (preview) {
      onApply(preview)
      onClose()
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="flex w-full max-w-2xl flex-col gap-4 rounded-xl border border-[#d9dfd5] bg-white p-6 shadow-xl">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Sparkles className="h-5 w-5 text-[#254632]" />
            <h2 className="text-base font-semibold text-[#172126]">Refinar com Gemini</h2>
          </div>
          <button
            onClick={onClose}
            className="rounded-md p-1 text-[#66746b] hover:bg-[#eef2eb] hover:text-[#172126]"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <AiModelConfig value={config} onChange={setConfig} compact />

        {preview ? (
          <div className="flex flex-col gap-2">
            <div className="text-xs font-medium uppercase tracking-wide text-[#66746b]">
              Preview do prompt refinado
            </div>
            <div className="max-h-64 overflow-y-auto rounded-md border border-[#d9dfd5] bg-[#f8faf7] p-3">
              <pre className="whitespace-pre-wrap text-sm text-[#172126]">{preview}</pre>
            </div>
          </div>
        ) : (
          <div className="rounded-md border border-[#d9dfd5] bg-[#f8faf7] p-3">
            <div className="text-xs font-medium uppercase tracking-wide text-[#66746b]">
              Conteudo atual ({content.length} caracteres)
            </div>
            <div className="mt-1 max-h-32 overflow-y-auto text-sm text-[#172126]">
              <pre className="whitespace-pre-wrap">{content.slice(0, 500)}{content.length > 500 ? '...' : ''}</pre>
            </div>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          {!preview ? (
            <Button
              type="button"
              onClick={() => refineMutation.mutate()}
              disabled={refineMutation.isPending || !content.trim()}
            >
              {refineMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Sparkles className="h-4 w-4" />
              )}
              Refinar
            </Button>
          ) : (
            <>
              <Button type="button" variant="secondary" onClick={() => setPreview(null)}>
                Tentar novamente
              </Button>
              <Button type="button" onClick={handleApply}>
                Aplicar
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
