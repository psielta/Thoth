import { ArrowDown, ArrowUp, Plus, Save, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import type { WorkflowActor } from '@/api/schemas'
import type { WorkflowPhaseInput } from '@/api/workflow'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { ACTOR_OPTIONS, PHASE_COLOR_PALETTE } from './constants'

export type PhaseDraft = { id: string | null; name: string; defaultActor: WorkflowActor; color: string }

type Row = PhaseDraft & { key: string }

export function PhaseEditor({
  initialPhases,
  onSave,
  saving,
  onCancel,
}: {
  initialPhases: PhaseDraft[]
  onSave: (phases: WorkflowPhaseInput[]) => void
  saving: boolean
  onCancel?: () => void
}) {
  const [rows, setRows] = useState<Row[]>(() => initialPhases.map((phase) => ({ key: crypto.randomUUID(), ...phase })))

  const update = (key: string, patch: Partial<Row>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)))
  const remove = (key: string) => setRows((current) => current.filter((row) => row.key !== key))
  const move = (index: number, direction: -1 | 1) =>
    setRows((current) => {
      const target = index + direction
      if (target < 0 || target >= current.length) {
        return current
      }
      const next = [...current]
      ;[next[index], next[target]] = [next[target], next[index]]
      return next
    })
  const add = () =>
    setRows((current) => [
      ...current,
      {
        key: crypto.randomUUID(),
        id: null,
        name: '',
        defaultActor: 'Human',
        color: PHASE_COLOR_PALETTE[current.length % PHASE_COLOR_PALETTE.length],
      },
    ])

  const canSave = rows.length > 0 && rows.every((row) => row.name.trim().length > 0)

  const save = () => {
    if (!canSave) {
      return
    }
    onSave(
      rows.map((row, index) => ({
        id: row.id,
        name: row.name.trim(),
        defaultActor: row.defaultActor,
        orderIndex: index,
        color: row.color,
      })),
    )
  }

  return (
    <div className="grid gap-2">
      {rows.map((row, index) => (
        <div key={row.key} className="flex items-center gap-2 rounded-md border border-border bg-card p-2">
          <input
            type="color"
            value={row.color}
            onChange={(event) => update(row.key, { color: event.target.value })}
            className="h-8 w-8 shrink-0 cursor-pointer rounded border border-border bg-card"
            aria-label="Cor da fase"
          />
          <Input
            value={row.name}
            onChange={(event) => update(row.key, { name: event.target.value })}
            placeholder="Nome da fase"
            className="flex-1"
          />
          <Select
            value={row.defaultActor}
            onChange={(event) => update(row.key, { defaultActor: event.target.value as WorkflowActor })}
            className="w-32"
          >
            {ACTOR_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
          <Button type="button" variant="ghost" size="icon" onClick={() => move(index, -1)} disabled={index === 0} aria-label="Subir">
            <ArrowUp className="h-4 w-4" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => move(index, 1)}
            disabled={index === rows.length - 1}
            aria-label="Descer"
          >
            <ArrowDown className="h-4 w-4" />
          </Button>
          <Button type="button" variant="ghost" size="icon" onClick={() => remove(row.key)} aria-label="Remover fase">
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}

      <div className="flex items-center gap-2">
        <Button type="button" variant="secondary" size="sm" onClick={add}>
          <Plus className="h-4 w-4" />
          Adicionar fase
        </Button>
        <div className="flex-1" />
        {onCancel ? (
          <Button type="button" variant="ghost" size="sm" onClick={onCancel}>
            <X className="h-4 w-4" />
            Cancelar
          </Button>
        ) : null}
        <Button type="button" size="sm" onClick={save} disabled={!canSave || saving}>
          <Save className="h-4 w-4" />
          Salvar fases
        </Button>
      </div>
    </div>
  )
}
