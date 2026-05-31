import { useQuery } from '@tanstack/react-query'
import { getAiModels } from '@/api/ai'
import { queryKeys } from '@/api/query-keys'

import { Select } from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import { Switch } from '@/components/ui/switch'
import { FormField } from '@/components/form-field'

export type ModelConfig = {
  model: string
  temperature: number
  thinkingEnabled: boolean
  thinkingBudget: number | null
  thinkingLevel: string | null
}

type AiModelConfigProps = {
  value: ModelConfig
  onChange: (next: ModelConfig) => void
  compact?: boolean
}

export function AiModelConfig({ value, onChange, compact }: AiModelConfigProps) {
  const modelsQuery = useQuery({
    queryKey: queryKeys.ai.models(),
    queryFn: getAiModels,
  })

  const models = modelsQuery.data ?? []
  const selected = models.find((m) => m.id === value.model)

  const handleModel = (modelId: string) => {
    const m = models.find((x) => x.id === modelId)
    if (!m) return

    // Reset thinking config when model changes
    const thinkingEnabled = m.canDisableThinking ? value.thinkingEnabled : true
    const thinkingBudget = m.thinkingMode === 'budget' ? (thinkingEnabled ? m.thinkingBudgetMin : 0) : null
    const thinkingLevel = m.thinkingMode === 'level' ? 'LOW' : null
    onChange({ ...value, model: modelId, thinkingEnabled, thinkingBudget, thinkingLevel })
  }

  const handleThinkingToggle = (enabled: boolean) => {
    if (!selected) return
    const thinkingBudget = selected.thinkingMode === 'budget'
      ? (enabled ? Math.max(selected.thinkingBudgetMin, 1024) : 0)
      : value.thinkingBudget
    onChange({ ...value, thinkingEnabled: enabled, thinkingBudget })
  }

  const handleBudget = (budget: number) => {
    onChange({ ...value, thinkingBudget: budget })
  }

  const thinkingVisible = selected && selected.thinkingMode !== 'none'
  const budgetVisible = thinkingVisible && selected.thinkingMode === 'budget' && value.thinkingEnabled

  return (
    <div className="flex flex-col gap-3">
      <div className={compact ? 'grid grid-cols-2 gap-3' : 'grid gap-3'}>
        <FormField label="Modelo">
          <Select
            value={value.model}
            onChange={(e) => handleModel(e.target.value)}
            disabled={modelsQuery.isPending}
          >
            {models.map((m) => (
              <option key={m.id} value={m.id}>
                {m.label}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField label={`Temperatura (${value.temperature.toFixed(1)})`}>
          <Slider
            min={0}
            max={2}
            step={0.1}
            value={value.temperature}
            onChange={(e) => onChange({ ...value, temperature: parseFloat(e.target.value) })}
          />
        </FormField>
      </div>

      {thinkingVisible ? (
        <div className="flex flex-col gap-2 rounded-md border border-[#d9dfd5] bg-[#f8faf7] p-3">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium text-[#172126]">Raciocinio</span>
            {selected.canDisableThinking ? (
              <Switch
                id="thinking-toggle"
                checked={value.thinkingEnabled}
                onChange={(e) => handleThinkingToggle(e.target.checked)}
                label={value.thinkingEnabled ? 'Ativado' : 'Desativado'}
              />
            ) : (
              <span className="text-xs text-[#66746b]">Sempre ativo</span>
            )}
          </div>

          {budgetVisible && selected.thinkingMode === 'budget' ? (
            <Slider
              label="Budget de tokens"
              showValue
              min={selected.thinkingBudgetMin}
              max={selected.thinkingBudgetMax}
              step={512}
              value={value.thinkingBudget ?? selected.thinkingBudgetMin}
              onChange={(e) => handleBudget(parseInt(e.target.value, 10))}
            />
          ) : null}

          {thinkingVisible && selected.thinkingMode === 'level' ? (
            <FormField label="Nivel de raciocinio">
              <Select
                value={value.thinkingLevel ?? 'LOW'}
                onChange={(e) => onChange({ ...value, thinkingLevel: e.target.value })}
              >
                <option value="LOW">Baixo</option>
                <option value="MEDIUM">Medio</option>
                <option value="HIGH">Alto</option>
              </Select>
            </FormField>
          ) : null}
        </div>
      ) : null}
    </div>
  )
}
