import { useMutation, useQuery } from '@tanstack/react-query'
import { AlertCircle, Loader2, RotateCcw, Sparkles, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { generateMermaidDiagram, getAiSettings } from '@/api/ai'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { GeneratedMermaid } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { AiModelConfig, type ModelConfig } from '../../prompts/ai/ai-model-config'

type GenerateMermaidDialogProps = {
  workingDirectoryId: string
  diagramId: string
  currentCode: string
  onApply: (code: string) => void
  onClose: () => void
}

const DEFAULT_CONFIG: ModelConfig = {
  model: 'gemini-3.5-flash',
  temperature: 0.4,
  thinkingEnabled: true,
  thinkingBudget: null,
  thinkingLevel: 'high',
}

const KIND_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: '', label: 'Automatico' },
  { value: 'flowchart', label: 'Flowchart' },
  { value: 'sequence', label: 'Sequence diagram' },
  { value: 'erd', label: 'ERD (entidade-relacionamento)' },
  { value: 'state', label: 'State diagram' },
  { value: 'class', label: 'Class diagram' },
  { value: 'mindmap', label: 'Mindmap' },
]

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function GenerateMermaidDialog({
  workingDirectoryId,
  diagramId,
  currentCode,
  onApply,
  onClose,
}: GenerateMermaidDialogProps) {
  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [config, setConfig] = useState<ModelConfig>(DEFAULT_CONFIG)
  const [instruction, setInstruction] = useState('')
  const [kind, setKind] = useState('')
  const [preview, setPreview] = useState<GeneratedMermaid | null>(null)
  const abortRef = useRef<AbortController | null>(null)

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

  useEffect(() => () => abortRef.current?.abort(), [])

  const generateMutation = useMutation({
    mutationFn: () => {
      const controller = new AbortController()
      abortRef.current = controller
      return generateMermaidDiagram(
        {
          instruction: instruction.trim(),
          diagramKind: kind ? kind : undefined,
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
          workingDirectoryId,
          diagramId,
          currentCode: currentCode.trim() ? currentCode : undefined,
        },
        controller.signal,
      )
    },
    onSuccess: (result) => {
      setPreview(result)
      toast.success(`Gerado — ${result.promptTokens} tokens entrada, ${result.candidateTokens} gerados.`)
    },
    onError: (err) => {
      if (!isAbortError(err)) {
        toast.error(getErrorMessage(err))
      }
    },
  })

  const handleApply = () => {
    if (preview?.mermaidCode) {
      onApply(preview.mermaidCode)
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
              <h2 className="text-sm font-semibold text-foreground">Gerar Mermaid com Gemini</h2>
              <p className="text-xs text-subtle-foreground">
                Descreva o diagrama. O codigo gerado aparece no editor e renderiza no preview antes de salvar.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted hover:text-foreground"
            aria-label="Fechar"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="rounded-lg border border-secondary bg-background p-4">
          <AiModelConfig value={config} onChange={setConfig} compact />
        </div>

        {!preview ? (
          <>
            <div className="grid gap-2">
              <label htmlFor="mermaid-ai-instruction" className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Instrucao
              </label>
              <Textarea
                id="mermaid-ai-instruction"
                value={instruction}
                onChange={(event) => setInstruction(event.target.value)}
                placeholder="Ex.: fluxo de login com validacao de credenciais e 2FA"
                className="min-h-24"
              />
            </div>

            <div className="grid gap-1.5 sm:max-w-xs">
              <label htmlFor="mermaid-ai-kind" className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Tipo de diagrama
              </label>
              <Select id="mermaid-ai-kind" value={kind} onChange={(event) => setKind(event.target.value)}>
                {KIND_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </div>
          </>
        ) : (
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between">
              <p className="text-xs font-medium uppercase tracking-wide text-primary">Codigo Mermaid gerado</p>
              <button
                type="button"
                onClick={() => setPreview(null)}
                className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
              >
                <RotateCcw className="h-3 w-3" />
                Gerar de novo
              </button>
            </div>
            <pre className="max-h-[45vh] overflow-auto rounded-lg border border-success-soft bg-card p-4 font-mono text-xs leading-relaxed text-foreground">
              {preview.mermaidCode}
            </pre>
            {preview.warnings.length > 0 ? (
              <div className="flex flex-col gap-1 rounded-md border border-warning-soft bg-background p-3">
                {preview.warnings.map((warning) => (
                  <div key={warning} className="flex items-start gap-2 text-xs text-warning-foreground">
                    <AlertCircle className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                    <span>{warning}</span>
                  </div>
                ))}
              </div>
            ) : null}
          </div>
        )}

        <div className="flex items-center justify-end gap-2">
          {!preview ? (
            <>
              <Button type="button" variant="secondary" onClick={onClose}>
                Cancelar
              </Button>
              {generateMutation.isPending ? (
                <Button type="button" variant="secondary" onClick={() => abortRef.current?.abort()}>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Cancelar geracao
                </Button>
              ) : (
                <Button type="button" onClick={() => generateMutation.mutate()} disabled={!instruction.trim()}>
                  <Sparkles className="h-4 w-4" />
                  Gerar
                </Button>
              )}
            </>
          ) : (
            <Button type="button" onClick={handleApply}>
              Aplicar no editor
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
