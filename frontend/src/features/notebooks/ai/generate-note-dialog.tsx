import { useMutation, useQuery } from '@tanstack/react-query'
import { FilePlus2, Loader2, Replace, RotateCcw, Sparkles, TextCursorInput, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { generateNoteMarkdown, getAiSettings } from '@/api/ai'
import { getErrorMessage } from '@/api/client'
import { queryKeys } from '@/api/query-keys'
import type { GeneratedNote } from '@/api/schemas'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { Textarea } from '@/components/ui/textarea'
import { AiModelConfig, type ModelConfig } from '../../prompts/ai/ai-model-config'
import { MarkdownContent } from '../../prompts/ai/markdown-content'

type GenerateNoteDialogProps = {
  notebookId: string
  workingDirectoryId: string | null
  currentContent: string
  onInsert: (result: GeneratedNote) => void
  onReplace: (result: GeneratedNote) => void
  onCreate: (result: GeneratedNote) => void
  onClose: () => void
}

const DEFAULT_CONFIG: ModelConfig = {
  model: 'gemini-3.5-flash',
  temperature: 0.5,
  thinkingEnabled: true,
  thinkingBudget: null,
  thinkingLevel: 'high',
}

const FORMAT_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: '', label: 'Livre (sem formato)' },
  { value: 'adr', label: 'ADR (decisao de arquitetura)' },
  { value: 'checklist', label: 'Checklist' },
  { value: 'ata', label: 'Ata de reuniao' },
  { value: 'resumo', label: 'Resumo' },
  { value: 'plano', label: 'Plano de implementacao' },
]

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function GenerateNoteDialog({
  notebookId,
  workingDirectoryId,
  currentContent,
  onInsert,
  onReplace,
  onCreate,
  onClose,
}: GenerateNoteDialogProps) {
  const settingsQuery = useQuery({
    queryKey: queryKeys.ai.settings(),
    queryFn: getAiSettings,
  })

  const [config, setConfig] = useState<ModelConfig>(DEFAULT_CONFIG)
  const [instruction, setInstruction] = useState('')
  const [format, setFormat] = useState('')
  const [useCurrentContent, setUseCurrentContent] = useState(false)
  const [preview, setPreview] = useState<GeneratedNote | null>(null)
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
      return generateNoteMarkdown(
        {
          instruction: instruction.trim(),
          format: format ? format : undefined,
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
          notebookId,
          currentContent: useCurrentContent && currentContent.trim() ? currentContent : undefined,
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

  const handleCancelGeneration = () => {
    abortRef.current?.abort()
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
              <h2 className="text-sm font-semibold text-foreground">Gerar nota com Gemini</h2>
              <p className="text-xs text-subtle-foreground">
                Descreva o que voce quer e revise antes de aplicar. Nada e salvo automaticamente.
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
              <label htmlFor="note-ai-instruction" className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                Instrucao
              </label>
              <Textarea
                id="note-ai-instruction"
                value={instruction}
                onChange={(event) => setInstruction(event.target.value)}
                placeholder="Ex.: crie uma nota de arquitetura sobre autenticacao"
                className="min-h-24"
              />
            </div>

            <div className="grid gap-2 sm:grid-cols-2">
              <div className="grid gap-1.5">
                <label htmlFor="note-ai-format" className="text-xs font-medium uppercase tracking-wide text-subtle-foreground">
                  Formato
                </label>
                <Select
                  id="note-ai-format"
                  value={format}
                  onChange={(event) => setFormat(event.target.value)}
                >
                  {FORMAT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="flex items-center rounded-md border border-secondary bg-background px-3 py-2 sm:self-end">
                <Switch
                  checked={useCurrentContent}
                  onChange={(event) => setUseCurrentContent(event.target.checked)}
                  label="Usar conteudo atual como contexto"
                  disabled={!currentContent.trim()}
                />
              </div>
            </div>

            {workingDirectoryId ? (
              <p className="text-xs text-subtle-foreground">
                Se o Contexto de IA estiver ativo no workspace deste bloco, ele sera usado na geracao.
              </p>
            ) : null}
          </>
        ) : (
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between">
              <p className="text-xs font-medium uppercase tracking-wide text-primary">
                {preview.suggestedTitle ? `Resultado · ${preview.suggestedTitle}` : 'Resultado gerado'}
              </p>
              <button
                type="button"
                onClick={() => setPreview(null)}
                className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
              >
                <RotateCcw className="h-3 w-3" />
                Gerar de novo
              </button>
            </div>
            <div className="max-h-[50vh] overflow-y-auto rounded-lg border border-success-soft bg-card p-4">
              <MarkdownContent content={preview.contentMarkdown} />
            </div>
          </div>
        )}

        <div className="flex items-center justify-end gap-2">
          {!preview ? (
            <>
              <Button type="button" variant="secondary" onClick={onClose}>
                Cancelar
              </Button>
              {generateMutation.isPending ? (
                <Button type="button" variant="secondary" onClick={handleCancelGeneration}>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Cancelar geracao
                </Button>
              ) : (
                <Button
                  type="button"
                  onClick={() => generateMutation.mutate()}
                  disabled={!instruction.trim()}
                >
                  <Sparkles className="h-4 w-4" />
                  Gerar
                </Button>
              )}
            </>
          ) : (
            <>
              <Button type="button" variant="secondary" onClick={() => onInsert(preview)}>
                <TextCursorInput className="h-4 w-4" />
                Inserir no cursor
              </Button>
              <Button type="button" variant="secondary" onClick={() => onReplace(preview)}>
                <Replace className="h-4 w-4" />
                Substituir
              </Button>
              <Button type="button" onClick={() => onCreate(preview)}>
                <FilePlus2 className="h-4 w-4" />
                Criar nova nota
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
