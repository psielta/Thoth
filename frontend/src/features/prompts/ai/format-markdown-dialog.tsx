import { useMutation, useQuery } from '@tanstack/react-query'
import { Heading, Loader2, RotateCcw, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { formatPromptMarkdown, getAiSettings } from '@/api/ai'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'
import { AiModelConfig, type ModelConfig } from './ai-model-config'
import { MarkdownContent } from './markdown-content'

type FormatMarkdownDialogProps = {
  content: string
  onApply: (formatted: string) => void
  onClose: () => void
}

const DEFAULT_CONFIG: ModelConfig = {
  model: 'gemini-3.5-flash',
  temperature: 0.2,
  thinkingEnabled: false,
  thinkingBudget: null,
  thinkingLevel: null,
}

export function FormatMarkdownDialog({ content, onApply, onClose }: FormatMarkdownDialogProps) {
  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [config, setConfig] = useState<ModelConfig>(DEFAULT_CONFIG)
  const [preview, setPreview] = useState<string | null>(null)

  const applied = useRef(false)
  useEffect(() => {
    if (settingsQuery.data && !applied.current) {
      applied.current = true
      setConfig({
        model: settingsQuery.data.model,
        temperature: 0.2,
        thinkingEnabled: settingsQuery.data.thinkingEnabled,
        thinkingBudget: settingsQuery.data.thinkingBudget ?? null,
        thinkingLevel: settingsQuery.data.thinkingLevel ?? null,
      })
    }
  }, [settingsQuery.data])

  const formatMutation = useMutation({
    mutationFn: () =>
      formatPromptMarkdown({
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
      toast.success(`Formatado — ${result.promptTokens} tokens entrada, ${result.candidateTokens} gerados.`)
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
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4 pt-16">
      <div className="flex w-full max-w-3xl flex-col gap-5 rounded-xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted">
              <Heading className="h-4 w-4 text-primary" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-foreground">Formatar em markdown</h2>
              <p className="text-xs text-subtle-foreground">
                O prompt atual sera convertido em Markdown estruturado
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted hover:text-foreground"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="rounded-lg border border-secondary bg-background p-4">
          <AiModelConfig value={config} onChange={setConfig} compact />
        </div>

        {!preview ? (
          <div className="flex flex-col gap-1.5">
            <p className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
              Conteudo atual · {content.length} caracteres
            </p>
            <div className="max-h-48 overflow-y-auto rounded-lg border border-secondary bg-card p-4">
              <pre className="whitespace-pre-wrap text-xs leading-relaxed text-muted-foreground">
                {content.length > 600 ? `${content.slice(0, 600)}\n…` : content}
              </pre>
            </div>
          </div>
        ) : (
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between">
              <p className="text-xs font-medium uppercase tracking-wide text-primary">Resultado formatado</p>
              <button
                type="button"
                onClick={() => setPreview(null)}
                className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
              >
                <RotateCcw className="h-3 w-3" />
                Tentar novamente
              </button>
            </div>
            <div className="max-h-[55vh] overflow-y-auto rounded-lg border border-success-soft bg-card p-4">
              <MarkdownContent content={preview} />
            </div>
          </div>
        )}

        <div className="flex items-center justify-between">
          <p className="text-xs text-subtle-foreground">
            Revise antes de aplicar. Mencoes @arquivo serao revalidadas.
          </p>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>
              Cancelar
            </Button>
            {!preview ? (
              <Button
                type="button"
                onClick={() => formatMutation.mutate()}
                disabled={formatMutation.isPending || !content.trim()}
              >
                {formatMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Heading className="h-4 w-4" />
                )}
                {formatMutation.isPending ? 'Formatando...' : 'Formatar'}
              </Button>
            ) : (
              <Button type="button" onClick={handleApply}>
                Aplicar no editor
              </Button>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}