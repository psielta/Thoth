# Plano 003 — Fluxo de fases + Timeline por Prompt (rastreador do dia a dia)

> Ao aprovar, salvar como `plan/003_prompt_workflow_timeline.md` (convenção de `plan/001`/`plan/002`). Feature nova sobre o app existente **PromptTasks**. **Não implementar antes da aprovação.**
> **v2** — revisado após feedback (rotas `/workspaces`, filtro `workflowStatus`, `WorkflowTemplate` com `OwnerId`, `WorkflowActor.ClaudeCode`, testes de front obrigatórios, guards de transição/edição).

## Contexto

Hoje o **PromptTasks** (.NET 10 Clean Architecture + React/Vite + PostgreSQL + SignalR) deixa criar/versionar prompts em Markdown por diretório, gerar prompts filhos e monitorar planos `.md`. Falta **gerenciar o estado do trabalho** em cima de cada tarefa.

O dia a dia do usuário tem um fluxo fixo, com loop: **Planejamento (Claude) → Revisão do plano (Codex) → Correção do plano (Claude) → … repete até aprovar → Implementação (Codex/Claude) → Revisão de código (Claude/Codex) → Teste prático (Você) → Commit/Merge (Você) → Concluída**. Como ele toca ~3 tarefas em paralelo, precisa: **definir a fase atual e o responsável**, ver uma **timeline** do histórico, e uma **visão geral** para retomar de onde parou.

**Decisões confirmadas com o usuário:**
- Construir **dentro** deste repo, no **backend existente** (Postgres/EF Core) — sem app novo, sem JSON em arquivo.
- A "tarefa" é o **próprio `Prompt`** (cada prompt **raiz/pai** = uma tarefa). Combina com `Prompt` já ter pai/filho e a lista principal ser `rootOnly`.
- v1 **sempre manual** (avança/volta/troca de fase; sem detecção automática).
- **Presets editáveis** (template padrão = o fluxo acima; editável em Configurações e por tarefa).
- **Board global** (home com tarefas de todos os diretórios, agrupadas por fase) **+ selos** de fase/responsável nas listas por workspace.
- **Rastreador + links contextuais** (timeline + prompts filhos + plano vinculado, com atalhos; manual, sem automação).
- **Aba "Timeline" no detalhe do prompt** (pedido explícito): em `/workspaces/$workspaceId/prompts/$promptId` (ex.: `/workspaces/019e7ab6-…/prompts/019e7ad4-…`).

## Princípios de modelagem (validados no código)

- **Estado atual materializado + log append-only** espelha `PromptVersion`/`LinkedDocumentVersion`. O **loop** revisão↔correção, "Voltar fase" e "Reabrir" são só eventos `PhaseChanged`/`Reopened`. O "agora" são campos materializados, sempre reconstruíveis pelos eventos.
- **Workflow em entidade 1‑1 separada** (`PromptWorkflow`), não em colunas no `Prompt`: concorrência (`xmin`) isolada das edições de conteúdo, impacto **aditivo** (nenhuma tabela existente alterada → baixo risco às features atuais).
- **Snapshot de fases por tarefa** (`PromptWorkflowPhase`, copiadas do template ao iniciar) desacopla edição do template das tarefas em andamento; eventos guardam `PhaseNameSnapshot` (histórico nunca quebra ao renomear/excluir fase).
- Reaproveitar o existente: `PromptMutationHelpers` (ownership via `Prompt.OwnerId`, `EnsureRowVersion`), `DtoMapper`, cadeia `IExceptionHandler` (400/404/409/500), "uma pasta por caso de uso" (MediatR+FluentValidation), `IPromptClient`/`SignalRPromptNotifier` + grupos `wd:{id}`; no front `ky`+Zod, `query-keys.ts`, `prompt-hub.tsx`, Vitest já configurado.

## Domínio e banco (migration `AddPromptWorkflow`)

Nova pasta `backend/src/PromptTasks.Domain/Workflows/`. PKs Guid v7 (geradas em app → permite montar FKs antes de um único `SaveChanges`), `DateTimeOffset`→`timestamptz` UTC, enums `HasConversion<int>()`, configs `IEntityTypeConfiguration<T>`.

**Enums:** `WorkflowActor { ClaudeCode=1, Codex=2, Human=3 }` *(alinhado com `TargetAgent.ClaudeCode`; rótulos UI: "Claude Code"/"Codex"/"Você")* · `PromptWorkflowStatus { Active=1, Done=2 }` · `WorkflowEventType { WorkflowStarted=1, PhaseChanged=2, ActorChanged=3, Note=4, Completed=5, Reopened=6, PhasesEdited=7 }`.

**Entidades:**
- `WorkflowTemplate : AuditableEntity` (template padrão **por dono**) — `Id, Name, IsDefault, OwnerId (NOT NULL = system em v1), CreatedAtUtc, UpdatedAtUtc`. Índice único parcial **por dono** do default (`OwnerId` where `IsDefault`). *(Resolve o risco multiusuário: o template não é global; em v1 é do system, semeado; sob demanda cria p/ o `currentUser` se faltar.)*
- `WorkflowTemplatePhase` — `Id, WorkflowTemplateId (cascade), Name, DefaultActor, OrderIndex, Color`.
- `PromptWorkflow` (1‑1 com Prompt) — `Id, PromptId (FK→Prompt cascade, **único**), Status, CurrentPhaseId?, CurrentPhaseName?, CurrentActor?, StartedAtUtc, EnteredCurrentPhaseAtUtc?, CreatedAtUtc, UpdatedAtUtc, RowVersion (xmin)`. `Current*` materializados → board barato.
- `PromptWorkflowPhase` (snapshot por tarefa) — `Id, PromptWorkflowId (cascade), Name, DefaultActor, OrderIndex, Color`.
- `PromptWorkflowEvent` (timeline append-only) — `Id, PromptWorkflowId (cascade), Type, PhaseId?, PhaseNameSnapshot?, Actor?, Note? (text), OccurredAtUtc`. Índice `(PromptWorkflowId, OccurredAtUtc)`.

**Template padrão (seed, por dono system)** — o fluxo do usuário, responsável editável depois:
1. Planejamento · ClaudeCode  2. Revisão do plano · Codex  3. Correção do plano · ClaudeCode  4. Implementação · Codex  5. Revisão de código · ClaudeCode  6. Teste prático · Human  7. Commit/Merge · Human. *("Concluída" = `Status=Done` via ação Concluir; não é fase.)*

**Alterar:** `ApplicationDbContext` + `IApplicationDbContext` (novos `DbSet`s/`IQueryable`s); `DbSeeder` (semear template default **do user system** — idempotente, junto do user); `DtoMapper`. Gerar `dotnet ef migrations add AddPromptWorkflow` → aplicar (dev faz `MigrateAsync` no startup). **Migration 100% aditiva** (só tabelas novas).

**Início do workflow (sem backfill destrutivo):**
- **Auto-start** em `CreatePromptHandler` quando `ParentPromptId == null` **e** `Status != Archived` (prompt arquivado já criado **não** inicia fluxo). A criação do `PromptWorkflow` + fases (snapshot) + eventos `WorkflowStarted`/`PhaseChanged` ocorre **no mesmo `SaveChangesAsync` do prompt** (atômico → nunca cria prompt sem workflow); `TaskWorkflowChanged` é notificado pós-commit.
- **Prompts raiz já existentes / arquivados** aparecem no board como **"Fluxo não iniciado"** com botão **Iniciar fluxo** (que aceita **fase inicial**, p/ posicionar tarefas em andamento). Prompts **filhos** não iniciam fluxo (são artefatos) → board não polui.

## Application (`Features/Workflow/`, padrão MediatR existente)

`WorkflowMutationHelpers` (espelha `PromptMutationHelpers`): carrega `PromptWorkflow`+`Prompt` validando `prompt.OwnerId == currentUser.UserId` (senão `NotFoundException`→404); `EnsureRowVersion`; `AppendEvent`; `RecomputeCurrentState`.

**Transições válidas (guards):** `advance/setPhase/changeActor/complete` exigem `Status=Active` (se `Done` → validação clara → 409/400); `reopen` exige `Status=Done`; **`AddWorkflowNote` é permitido em qualquer status** (Active ou Done — nota é append-only, útil p/ retrospectiva) e **não** exige `rowVersion` (só insere filho, não muda a row).

**Commands** (carregar → guard/rowVersion → anexar evento → recomputar → `SaveChanges` → DTO → notificar pós-commit):
`StartWorkflow` (copia fases do template do dono; eventos `WorkflowStarted`+`PhaseChanged`; fase inicial opcional) · `AdvancePhase` (próximo `OrderIndex`) · `SetPhase` (qualquer fase — cobre **Voltar** e o **loop**) · `ChangeActor` · `AddWorkflowNote` · `CompleteWorkflow` · `ReopenWorkflow` (fase alvo opcional) · `UpdateTaskPhases` · `UpdateWorkflowTemplate`.
**Validators (pt-BR):** edição de fases (`UpdateTaskPhases`/`UpdateWorkflowTemplate`) valida **≥1 fase, nome não vazio, `OrderIndex` contíguo sem duplicatas, `Color` hex válida**; **bloquear excluir a fase atual ou fase com eventos** (oferecer renomear/reordenar; histórico preservado por `PhaseNameSnapshot`).

**Queries:** `GetWorkflow(promptId)` (workflow + fases + timeline completa) · `GetWorkflowBoard(workflowStatus?, promptStatus?, workingDirectoryId?, q?)` → `TaskSummaryDto[]` de **prompts raiz** do dono via LEFT JOIN workflow (inclui "não iniciados") · `GetWorkflowTemplate()` (default do dono).

**DTOs** (`Common/Models`, mapeados em `DtoMapper`): `WorkflowDto`, `WorkflowPhaseDto`, `WorkflowEventDto`, `WorkflowTemplateDto`, `TaskSummaryDto(promptId, title, workingDirectoryId, workingDirectoryName, promptStatus, workflowStatus?, currentPhaseId?, currentPhaseName?, currentActor?, enteredCurrentPhaseAtUtc?, updatedAtUtc, hasChildPrompts, hasLinkedPlan, rowVersion?)`. Enums viajam como string (config Newtonsoft atual).

## API (REST `/api`, Newtonsoft camelCase, ProblemDetails)

Novo `Api/Controllers/WorkflowController.cs` (fino, `ISender`):
- `GET/POST /api/prompts/{promptId}/workflow` (obter / iniciar)
- `POST .../workflow/advance` · `.../phase` · `.../actor` · `.../notes` · `.../complete` · `.../reopen`
- `PUT /api/prompts/{promptId}/workflow/phases` (editar fases da tarefa)
- `GET /api/workflow/board?workflowStatus=&promptStatus=&workingDirectoryId=&q=`
- `GET|PUT /api/workflow/template`

**Filtros do board (desambiguados):** `workflowStatus ∈ {Active,Done}` (estado do fluxo) e `promptStatus ∈ {Draft,Ready,Archived}` são **separados**. **Default: ocultar prompts `Archived`**; para vê-los, `promptStatus=Archived` (toggle "incluir arquivados" na UI). Conflito `xmin`→409 (`ConflictExceptionHandler`); ações que exigem `rowVersion`: advance/phase/actor/complete/reopen/phases.

## SignalR (mesma conexão `/hubs/prompts`)

- `IPromptClient` += `TaskWorkflowChanged(TaskSummaryDto summary)` (payload leve, **sem a timeline**).
- Novo grupo global **`tasks:all`** + métodos `JoinTasks()`/`LeaveTasks()` no `PromptHub` (board cruza diretórios). Broadcast vai p/ **`wd:{Prompt.WorkingDirectoryId}`** (detalhe) **e** `tasks:all` (board).
- Novo `IWorkflowNotifier` (Application) + `Api/Realtime/SignalRWorkflowNotifier.cs` (espelha `SignalRPromptNotifier`); chamado **pós-commit**. Notifiers de prompt/linked-document intocados.

## Frontend (React + TanStack, padrões existentes)

**Rotas (corrige a navegação — hoje a tela de diretórios é o `/`):**
- Criar `src/routes/workspaces/index.tsx` (`/workspaces`) com a tela atual de diretórios (mover o corpo do `index.tsx`: `WorkspaceForm` + `WorkspaceList` + heading).
- `src/routes/index.tsx` (`/`) passa a renderizar o **Board**.
- `src/routes/workspaces/$workspaceId.tsx`: botão "voltar" de `to="/"` → **`to="/workspaces"`** (rótulo "Diretórios"). `WorkspaceList` já linka `/workspaces/$workspaceId` (sem mudança).
- `src/routes/__root.tsx`: nav no header **Board** (`/`) **| Diretórios** (`/workspaces`).
- `routeTree.gen.ts` é **gerado** pelo `@tanstack/router-plugin` (rodar dev/build p/ regenerar; **não** editar à mão; validar que compila).

**Dados/realtime:** `src/api/schemas.ts` (+ `workflowActor` = `z.enum(['ClaudeCode','Codex','Human'])`, `promptWorkflowStatus`, `workflowEventType`, `workflowPhase/workflowEvent/workflow/taskSummary/workflowTemplate` + listas + types) · `src/api/query-keys.ts` (+ `workflow: { board(filters), detail(promptId), template() }`) · novo `src/api/workflow.ts` (chamadas com `.parse()` Zod) · `src/realtime/prompt-hub.tsx` (handler `TaskWorkflowChanged` → atualiza cache do board + `invalidate workflow.detail` + `invalidate prompts.all`; expõe `joinTasks/leaveTasks`, re-join no `onreconnected`).

**Board global (home `/`):** `src/features/workflow/`:
- `board.tsx` — `getBoard`, agrupa em **Sem fluxo · [fases] · Concluídas (Done)**; filtros (diretório / `q` / `workflowStatus` / toggle "incluir arquivados"); join `tasks:all` no mount.
- `task-card.tsx` — título, nome do diretório, **selo de fase**, **selo de responsável** ("Vez de: Codex"; destaque quando = Você), **tempo na fase** ("há 2h" via `Intl.RelativeTimeFormat` — sem dep nova), ações rápidas (Avançar, Nota), link ao detalhe.

**Aba "Timeline" no detalhe do prompt:** 4ª aba (rótulo **Timeline**) em `src/routes/workspaces/$workspaceId/prompts/$promptId.tsx`, renderizando `src/features/workflow/workflow-panel.tsx`. **A timeline vertical é o centro da aba**, com cabeçalho de estado + ações acima:
- ações: **Avançar fase · Voltar/Mudar fase (select) · Mudar responsável · Adicionar nota · Concluir/Reabrir**;
- `workflow-timeline.tsx` (eventos: fase/responsável/quando/nota) — **peça principal**;
- `phase-editor.tsx` (editar fases desta tarefa; reaproveitado em Configurações);
- **links contextuais**: resumo de **prompts filhos** (dados de `PromptChildrenPanel`) e **plano vinculado** (`LinkedDocumentsPanel`/`linked-documents`), com botões que trocam para as abas existentes;
- estado "Fluxo não iniciado" → **Iniciar fluxo** (com escolha de fase inicial).

**Configurações:** `src/routes/settings.tsx` (`/configuracoes`) editando o template default do dono (mesmo `phase-editor.tsx`).

**Selos nas listas:** `src/features/prompts/prompt-list.tsx` consulta `getBoard({workingDirectoryId})` e mostra **fase + responsável** em cada card (sem alterar o endpoint de prompts).

**shadcn a adicionar** (manter tema/CSS atuais): `card dialog alert-dialog tooltip scroll-area dropdown-menu`. Datas via `Intl` nativo (sem date-fns).

## Plano de implementação (cada etapa compila + testes verdes)

0. **Domínio + migration**: enums + 5 entidades + configs + `DbSet`s + `IApplicationDbContext` + seed do template (system) em `DbSeeder`; `dotnet ef migrations add AddPromptWorkflow` → `database update`.
1. **`WorkflowMutationHelpers` + projeção** + **unit tests** (loop; voltar; reabrir; tempo-na-fase; guards de transição; `Note` em Done).
2. **Commands/Queries/DTOs/`DtoMapper`/validators** (inclui validação de fases) + **`WorkflowController`** (notifier stub) + **integração** dos endpoints (start/advance/phase/actor/note/complete/reopen/board com `workflowStatus`+`promptStatus`/template + 409).
3. **`IWorkflowNotifier` + `SignalRWorkflowNotifier`** + `TaskWorkflowChanged`/grupo `tasks:all`; ligar nos handlers (pós-commit) + **auto-start atômico** em `CreatePromptHandler` (raiz e não-arquivado). **Integração** do broadcast.
4. **Front — rotas**: criar `/workspaces`, mover diretórios, Board em `/`, header nav, back button `→ /workspaces`, regenerar `routeTree.gen.ts`.
5. **Front — dados**: schemas, query-keys, `api/workflow.ts`, handler em `prompt-hub.tsx`.
6. **Front — Board** (`board`, `task-card`, agrupamento, filtros) + selos em `prompt-list.tsx`.
7. **Front — aba "Timeline" no detalhe** (timeline + estado/ações + links contextuais + Iniciar fluxo).
8. **Front — Configurações** (template) + `phase-editor` compartilhado.
9. **Testes de front (obrigatórios, Vitest)** + **E2E manual/browser** + polish (empty states, confirmações, destaque "minha vez").

## Critérios de validação (comandos exatos)

```powershell
docker compose up -d
dotnet build backend/PromptTasks.sln
dotnet test  backend/PromptTasks.sln          # unit + integração (Testcontainers)
dotnet run --project backend/src/PromptTasks.Api/PromptTasks.Api.csproj   # http://localhost:5080/scalar
cd frontend; npm run test; npm run lint; npm run build; npm run dev        # http://localhost:5173
```
Fluxo de produto: home (`/`) abre o **Board**; "Diretórios" (`/workspaces`) lista os diretórios e o "voltar" do workspace volta p/ `/workspaces` → criar tarefa (raiz, não arquivada) nasce em **Planejamento/Claude Code** no Board → Avançar até Revisão (Codex) → **Correção→Revisão (loop)** com cada transição na timeline (horário/responsável) → trocar responsável na Implementação → **nota** (colar feedback do Codex), inclusive depois de concluir → abrir a aba **Timeline** no detalhe e ver toda a timeline + prompts filhos + plano vinculado com atalhos → 2ª aba do navegador reflete **ao vivo** (SignalR) no Board e no detalhe → **reiniciar a API** e recarregar (persistiu no Postgres) → **Concluir** (vai p/ "Concluídas") e **Reabrir** → em **Configurações** renomear/reordenar fase (tarefas existentes intactas; nova tarefa usa o novo template) → tentar excluir a fase atual / com eventos (bloqueado) → prompt antigo/arquivado mostra **Iniciar fluxo** e posiciona na fase certa → toggle "incluir arquivados" no board.

## Riscos e mitigações

- **Excluir/renomear fase com histórico** → `PhaseNameSnapshot` + validator bloqueia excluir fase atual/com eventos.
- **Template multiusuário** → `WorkflowTemplate.OwnerId` (NOT NULL = system em v1), default por dono, criado sob demanda; nada global editável.
- **Ambiguidade de status** → board separa `workflowStatus` (Active/Done) de `promptStatus` (Draft/Ready/Archived); Archived oculto por padrão.
- **Concorrência** → `PromptWorkflow.RowVersion` (xmin) exigido nas mutações de estado → 409; `Note` não conflita.
- **Atomicidade do auto-start** → workflow criado no mesmo `SaveChangesAsync` do prompt; só p/ raiz não-arquivado.
- **Board ao vivo cruzando diretórios** → grupo `tasks:all` (re-join no reconnect, como `wd:`).
- **Rotas** → mover diretórios p/ `/workspaces` antes de pôr Board em `/`; `routeTree.gen.ts` é gerado (validar build).
- **Não acoplar** com `Prompt.Status`; Concluir não arquiva o prompt (futuro opcional).
- **Sem novas deps** (NuGet: BCL; npm: só componentes shadcn + `Intl`). Enums como string casam com os `z.enum` do front (`ClaudeCode/Codex/Human`).

## Testes (backend + frontend — frontend obrigatório)

- **`Application.UnitTests`** (`*Workflow*`): helpers/handlers — `StartWorkflow` copia fases + eventos iniciais; loop revisão↔correção e estado materializado; `AdvancePhase`/`SetPhase`/`ChangeActor`; `AddWorkflowNote` (sem rowVersion, permitido em Done); `Complete`/`Reopen` (guards); `UpdateTaskPhases`/`UpdateWorkflowTemplate` (≥1 fase, nome/OrderIndex/cor, bloqueio de excluir fase atual/com eventos); notifier chamado pós-commit. Fakes (`ICurrentUser`/`IDateTimeProvider`/`IWorkflowNotifier`, `IApplicationDbContext` InMemory/SQLite).
- **`Api.IntegrationTests`** (Testcontainers): `WorkflowFlowTests` — auto-start na criação de raiz não-arquivada; advance/loop/note/complete/reopen; `GET workflow` com timeline; **409** em rowVersion velho; **board** com filtros `workflowStatus`/`promptStatus` (arquivado oculto por padrão); editar template → nova tarefa usa fases novas; **SignalR** `TaskWorkflowChanged` (TaskCompletionSource + timeout) em `wd:{id}` e `tasks:all`.
- **Frontend (Vitest — obrigatório)**: `board.test.tsx` (agrupamento Sem fluxo/fases/Concluídas + filtros), `workflow-timeline.test.tsx` (ordem/rotulagem dos eventos), `workflow-panel.test.tsx` (ações Avançar/Voltar/Mudar responsável/Nota/Concluir disparam as mutations certas, api mockada). Reusar `src/test/setup.ts`.

## Arquivos críticos

**Backend — criar:** `Domain/Workflows/{WorkflowActor,PromptWorkflowStatus,WorkflowEventType,WorkflowTemplate,WorkflowTemplatePhase,PromptWorkflow,PromptWorkflowPhase,PromptWorkflowEvent}.cs` · `Infrastructure/Persistence/Configurations/{WorkflowTemplate,WorkflowTemplatePhase,PromptWorkflow,PromptWorkflowPhase,PromptWorkflowEvent}Configuration.cs` · `Infrastructure/Persistence/Migrations/*_AddPromptWorkflow.cs` · `Application/Features/Workflow/**` (commands/queries/validators) + `WorkflowMutationHelpers.cs` · `Application/Common/Models/{WorkflowDto,WorkflowPhaseDto,WorkflowEventDto,WorkflowTemplateDto,TaskSummaryDto}.cs` · `Application/Common/Realtime/IWorkflowNotifier.cs` · `Api/Controllers/WorkflowController.cs` · `Api/Realtime/SignalRWorkflowNotifier.cs`.
**Backend — alterar:** `Application/Common/Interfaces/IApplicationDbContext.cs` · `Infrastructure/Persistence/ApplicationDbContext.cs` · `Infrastructure/Persistence/DbSeeder.cs` · `Application/Common/Mappings/DtoMapper.cs` · `Application/Common/Realtime/IPromptClient.cs` · `Api/Hubs/PromptHub.cs` · `Application/Features/Prompts/Commands/CreatePrompt/CreatePromptHandler.cs` · DI (Infrastructure/Api).
**Frontend — criar:** `src/routes/workspaces/index.tsx` (diretórios) · `src/api/workflow.ts` · `src/features/workflow/{board,task-card,workflow-panel,workflow-timeline,phase-editor,constants}.tsx` · `src/routes/settings.tsx` · testes Vitest (board/timeline/panel) · componentes shadcn novos.
**Frontend — alterar:** `src/routes/index.tsx` (→ Board) · `src/routes/__root.tsx` (nav) · `src/routes/workspaces/$workspaceId.tsx` (voltar → `/workspaces`) · `src/routes/workspaces/$workspaceId/prompts/$promptId.tsx` (aba Timeline) · `src/api/{schemas.ts,query-keys.ts}` · `src/realtime/prompt-hub.tsx` · `src/features/prompts/prompt-list.tsx` · `routeTree.gen.ts` (regenerado).
