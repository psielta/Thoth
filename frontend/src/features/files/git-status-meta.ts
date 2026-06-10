import type { GitFileStatusValue } from '@/api/schemas'

type GitStatusMeta = {
  letter: string
  label: string
  badgeClass: string
}

const GIT_STATUS_META: Record<GitFileStatusValue, GitStatusMeta> = {
  Modified: {
    letter: 'M',
    label: 'Modificado',
    badgeClass: 'bg-warning-soft text-warning-foreground',
  },
  Added: {
    letter: 'A',
    label: 'Adicionado',
    badgeClass: 'bg-success-soft text-success-foreground',
  },
  Deleted: {
    letter: 'D',
    label: 'Excluido',
    badgeClass: 'bg-danger-soft text-danger-soft-foreground',
  },
  Renamed: {
    letter: 'R',
    label: 'Renomeado',
    badgeClass: 'bg-info-soft text-info-foreground',
  },
  Untracked: {
    letter: 'U',
    label: 'Nao rastreado',
    badgeClass: 'bg-success-soft text-success-foreground',
  },
}

export function getGitStatusMeta(status: GitFileStatusValue) {
  return GIT_STATUS_META[status]
}
