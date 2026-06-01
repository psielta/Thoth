import { useMutation, useQuery } from '@tanstack/react-query'
import { Loader2, RotateCcw, Sparkles, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { getAiSettings, refinePrompt } from '@/api/ai'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import { Button } from '@/components/ui/button'
import { PromptEditor } from '../prompt-editor'
import { AiModelConfig, type ModelConfig } from './ai-model-config'
import { ContextFilePicker } from './context-file-picker'
import { MarkdownContent } from './markdown-content'

type RefineDialogProps = {
  content: string
  workingDirectoryId: string
  onApply: (refined: string) => void
  onClose: () => void
}

const DEFAULT_CONFIG: ModelConfig = {
  model: 'gemini-3.5-flash',
  temperature: 0.4,
  thinkingEnabled: true,
  thinkingBudget: null,
  thinkingLevel: 'high',
}

export function RefineDialog({ content, workingDirectoryId, onApply, onClose }: RefineDialogProps) {
  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [config, setConfig] = useState<ModelConfig>(DEFAULT_CONFIG)
  const [preview, setPreview] = useState<string | null>(null)
  const [contextFiles, setContextFiles] = useState<string[]>([])
  const [customInstructions, setCustomInstructions] = useState('')

  const applied = useRef(false)
  useEffect(() => {
    if (settingsQuery.data && !applied.current) {
      applied.current = true
      setConfig({
        model: settingsQuery.data.model,
        temperature: settingsQuery.data.temperature,
        thinkingEnabled: settingsQuery.data.thinkingEnabled,
        thinkingBudget: settingsQuery.data.thinkingBudget ?? null,
        thinkingLevel: settingsQuery.data.thinkingLevel ?? null,
      })
    }
  }, [settingsQuery.data])

  const refineMutation = useMutation({
    mutationFn: () => {
      const trimmedCustomInstructions = customInstructions.trim()

      return refinePrompt({
        content,
        model: config.model,
        temperature: config.temperature,
        thinkingMode: config.thinkingEnabled
          ? config.thinkingBudget != null ? 'budget'
          : config.thinkingLevel != null ? 'level'
          : 'none'
          : 'none',
        thinkingBudget: config.thinkingEnabled ? config.thinkingBudget : null,
        thinkingLevel: config.thinkingEnabled ? config.thinkingLevel : null,
        workingDirectoryId,
        contextFiles,
        customInstructions: trimmedCustomInstructions.length > 0 ? trimmedCustomInstructions : undefined,
      })
    },
    onSuccess: (result) => {
      setPreview(result.content)
      toast.success(`Refinado - ${result.promptTokens} tokens entrada, ${result.candidateTokens} gerados.`)
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
              <Sparkles className="h-4 w-4 text-primary" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-foreground">Refinar com Gemini</h2>
              <p className="text-xs text-subtle-foreground">O prompt atual sera otimizado pela IA</p>
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
          <>
            <div className="flex flex-col gap-1.5">
              <p className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Conteudo atual - {content.length} caracteres
              </p>
              <div className="max-h-48 overflow-y-auto rounded-lg border border-secondary bg-card p-4">
                <pre className="whitespace-pre-wrap text-xs leading-relaxed text-muted-foreground">
                  {content.length > 600 ? `${content.slice(0, 600)}\n...` : content}
                </pre>
              </div>
            </div>

            <div className="grid gap-2">
              <p className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Arquivos de contexto (opcional)
              </p>
              <ContextFilePicker
                workingDirectoryId={workingDirectoryId}
                value={contextFiles}
                onChange={setContextFiles}
              />
            </div>

            <div className="grid gap-2">
              <p className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Instrucoes de refinamento (opcional)
              </p>
              <PromptEditor
                workingDirectoryId={workingDirectoryId}
                value={customInstructions}
                onChange={(markdown) => setCustomInstructions(markdown)}
                contentClassName="max-h-36 overflow-y-auto"
                editorClassName="min-h-24"
              />
            </div>
          </>
        ) : (
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between">
              <p className="text-xs font-medium uppercase tracking-wide text-primary">
                Resultado refinado
              </p>
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
                onClick={() => refineMutation.mutate()}
                disabled={refineMutation.isPending || !content.trim()}
              >
                {refineMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Sparkles className="h-4 w-4" />
                )}
                {refineMutation.isPending ? 'Refinando...' : 'Refinar'}
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
