# Plano 002 — Vincular & monitorar markdown externo (plan mode do Claude Code) a um Prompt

> Ao aprovar, salvar este plano no repo como `plan/002_linked_external_markdown.md` (segue a convenção de `plan/001_initial_plan.md`). Plano para outro agente validar e implementar. **Não implementar ainda.**

## Contexto

O sistema já existe e funciona (backend .NET 10 Clean Architecture + frontend React, conforme `plan/001_initial_plan.md`, **implementado**). Quero, na tela de detalhe do prompt, **vincular um arquivo `.md` de plano gerado pelo Claude Code** (ex.: `C:\Users\psiel\.claude\plans\...md`, normalmente **fora** do workspace), renderizá-lo, **monitorar alterações no disco**, **versionar automaticamente** o conteúdo, e **atualizar a tela em tempo real via SignalR** — sem misturar com os eventos de `PromptUpdated`. Deve suportar 1..N documentos por prompt e ser extensível p/ outros tipos de documento no futuro.

---

## 1. Diagnóstico do estado atual (verificado no código)

**Já existe e será reaproveitado:**
- `Domain/Prompts/LinkedDocument.cs` — **entidade JÁ totalmente mapeada e migrada** (não é stub): `Id` (Guid v7), `PromptId` (FK→Prompt, **cascade**), `WorkingDirectoryId` (FK→WD, **restrict, NOT NULL**), `RelativePath` (varchar 1024, NOT NULL), `Status` (`LinkedDocumentStatus` int), `LastContentHash` (varchar 128, null), `CreatedAtUtc`/`UpdatedAtUtc` (`DateTimeOffset`, setados manualmente — **não** é `AuditableEntity`). Navs `Prompt`, `WorkingDirectory`. Tabela `linked_documents`. Exposta em `IApplicationDbContext.LinkedDocuments`, em `ApplicationDbContext`, e em `Prompt.LinkedDocuments`.
- `Domain/Prompts/LinkedDocumentStatus.cs` — `Draft=1, Tracking=2, Paused=3` (faltam **Error** e **Missing**).
- **Padrão de versionamento** (`PromptVersion` + `PromptMutationHelpers.CreateVersion`): `VersionNumber = prompt.CurrentVersion`, snapshot, índice único `(PromptId, VersionNumber)`. **Nenhum hashing de conteúdo hoje.** Vou espelhar isso num `LinkedDocumentVersion`.
- **SignalR**: `Application/Common/Realtime/IPromptClient.cs` (`PromptCreated/Updated/Deleted`), `Api/Hubs/PromptHub.cs` (`Hub<IPromptClient>`, grupos `wd:{workingDirectoryId}`, `JoinWorkingDirectory/LeaveWorkingDirectory(Guid)`), `IPromptNotifier`→`Api/Realtime/SignalRPromptNotifier.cs` (injeta `IHubContext<PromptHub, IPromptClient>`, **Scoped**). **Publicação é pós-commit, por chamada direta nos handlers** (sem MediatR notifications, sem interceptor, sem domain events).
- **Path-safety**: `IWorkspaceFileService` + `Infrastructure/FileSystem/WorkspaceFileService.cs` (helpers `CanonicalizeExistingPath`, `EnsureContained`, etc.). **Não há FileSystemWatcher nem BackgroundService em lugar nenhum.**
- **Auditoria** é manual em `ApplicationDbContext.SaveChangesAsync` (`ApplyAuditing`) só p/ `AuditableEntity`; `LinkedDocument` não é auditável.
- **DI**: `IPromptNotifier` Scoped (Api), `IWorkspaceFileService`/`IApplicationDbContext` Scoped, `IDateTimeProvider` Singleton, `AddMemoryCache()`. **Nenhum `AddHostedService`.** CORS policy `"spa"` com `AllowCredentials()`. Hub em `/hubs/prompts`. Controllers finos (`ISender`), `rowVersion` no corpo, erros via `IExceptionHandler` (`ConflictExceptionHandler`→409, etc.). DTOs mapeados manualmente em `Common/Mappings/DtoMapper.cs`.

**Frontend (verificado):**
- `src/api/{client.ts (ky, base `http://localhost:5080/api`), query-keys.ts, schemas.ts (zod), prompts.ts, files.ts}`; `src/realtime/prompt-hub.tsx` (`HubConnectionBuilder().withAutomaticReconnect()`, join `wd:{id}`, eventos→cache via `setQueryData`/`invalidateQueries`, `onreconnected` re-join; expõe `usePromptHub() → {connected, joinWorkingDirectory, leaveWorkingDirectory}`). `useQuery`/`useMutation` (**sem loaders**, sem suspense). Query keys `queryKeys.prompts.{all,list,detail,versions}`.
- Detalhe do prompt `routes/workspaces/$workspaceId/prompts/$promptId.tsx` = grid `[PromptForm | PromptVersions sidebar]`. `PromptVersions` é uma lista simples (sem dialog/diff).
- **Não existe renderizador de markdown** (Tiptap é só editor). `components/ui/` tem só **button, input, label, select, textarea, badge, separator** (+ `form-field.tsx`). Tema verde-escuro com classes Tailwind inline. Toaster = sonner.

**Lacunas a preencher:** `LinkedDocument` precisa **evoluir** (path absoluto fora do workspace, mais status, tipo, erro, versão, tamanho); falta tabela de **versões do documento**; faltam **commands/queries/controller/DTOs**; falta **serviço de arquivo externo** (validação + leitura + hash); falta **watcher/BackgroundService**; faltam **eventos SignalR de documento** (separados de prompt); falta **toda a UI** + **renderizador de markdown**.

**Decisão-chave:** **evoluir** `LinkedDocument` (não criar entidade nova) — ela já está posicionada para isso; o que falta são colunas e comportamento.

---

## 2. Modelo de domínio e banco proposto

### 2.1 Evoluir `LinkedDocument` (migration `AddLinkedDocumentTracking`)
Como a feature está **sem uso real** hoje (sem handlers), a migration pode ajustar colunas sem backfill:

| Mudança | Detalhe |
|---|---|
| `RelativePath` → **`AbsolutePath`** | `RenameColumn`; varchar **1024**, NOT NULL; guarda o caminho **absoluto real** (p/ exibição/abertura; arquivo pode estar fora do WD). |
| **+`AbsolutePathKey`** | varchar 1024, NOT NULL — **chave normalizada** p/ unicidade e match (FSW/dir): **lowercase** (Windows é case-insensitive) + separadores padronizados (`\`) + sem barra final. Evita duplicar `C:\Users\X\a.md` × `c:\users\x\A.md`. `AbsolutePath` = real; `AbsolutePathKey` = só chave/comparação. |
| `WorkingDirectoryId` → **nullable** (`Guid?`) | FK restrict mantido; agora opcional ("workspace de origem"). **O grupo SignalR usa `Prompt.WorkingDirectoryId`** (sempre presente), não este campo. |
| **+`DocumentType`** | enum int NOT NULL, default `ClaudeCodePlan=1` (extensibilidade — requisito 2). |
| **+`DisplayName`** | varchar 260, null; default = nome do arquivo. |
| **+`CurrentVersion`** | int NOT NULL default 0. |
| **+`LastError`** | varchar 1024, null (mensagem do último erro p/ `Error`/`Missing`). |
| **+`LastSyncedAtUtc`** | timestamptz, null (última leitura bem-sucedida). |
| **+`SizeBytes`** | bigint, null (último tamanho conhecido). |
| `LastContentHash` | mantido (varchar 128 cabe SHA-256 hex = 64). |
| **Índices** | `+ IX (Status)` (consulta de startup do watcher); **`+ unique (PromptId, AbsolutePathKey)`** (não vincular o mesmo arquivo 2x no mesmo prompt — usa a **chave normalizada**, não o path bruto, p/ não escapar por diferença de caixa/separador). |

`LinkedDocumentStatus` passa a: `Draft=1, Tracking=2, Paused=3, **Error=4**, **Missing=5**` (valores int — adicionar membros não exige DDL). Novo enum `LinkedDocumentType { ClaudeCodePlan = 1 }` em `Domain/Prompts/`.

### 2.2 Nova entidade `LinkedDocumentVersion` (tabela `linked_document_versions`)
Versionamento do **conteúdo do documento**, **separado** de `PromptVersion`:

`Id` (Guid v7) · `LinkedDocumentId` (FK→LinkedDocument, **cascade**) · `VersionNumber` (int) · `Content` (`text`) · `ContentHash` (varchar 64, SHA-256 hex) · `SizeBytes` (bigint) · `Source` (enum int: `Initial=1, FileChanged=2, ManualRefresh=3, Resumed=4`) · `CreatedAtUtc` (timestamptz). **Único `(LinkedDocumentId, VersionNumber)`**; índice `(LinkedDocumentId, CreatedAtUtc desc)`.

Arquivos a criar (seguindo convenção existente): `Domain/Prompts/LinkedDocumentVersion.cs`, `Domain/Prompts/LinkedDocumentVersionSource.cs`, `Infrastructure/Persistence/Configurations/LinkedDocumentVersionConfiguration.cs`; novo `DbSet<LinkedDocumentVersion>` em `ApplicationDbContext` + `IApplicationDbContext`; nav `LinkedDocument.Versions`. **Conteúdo "atual" = última `LinkedDocumentVersion`** (fonte única; `LinkedDocument` guarda só metadados). Snapshot completo por versão (espelha `PromptVersion`; planos não são grandes — aceitável; risco de crescimento em §10).

### 2.3 DTOs (`Application/Common/Models`, mapeados em `DtoMapper`)
- `LinkedDocumentDto(Id, PromptId, WorkingDirectoryId?, AbsolutePath, DisplayName, DocumentType, Status, CurrentVersion, LastContentHash?, SizeBytes?, LastError?, LastSyncedAtUtc?, CreatedAtUtc, UpdatedAtUtc)` — **metadados, sem conteúdo**.
- `LinkedDocumentContentDto(LinkedDocumentId, VersionNumber, Content, ContentHash, SizeBytes, CreatedAtUtc)`.
- `LinkedDocumentVersionDto(Id, LinkedDocumentId, VersionNumber, ContentHash, SizeBytes, Source, CreatedAtUtc)` — **sem `Content`** (lista leve).

---

## 3. Contratos de API (REST, base `/api`, Newtonsoft camelCase, erros ProblemDetails)

Novo `Api/Controllers/LinkedDocumentsController.cs` (attribute routing, finos via `ISender`). Features em `Application/Features/LinkedDocuments/`. **Reaproveita os `IExceptionHandler` já existentes** (`ValidationExceptionHandler`→**400**, `NotFoundExceptionHandler`→**404**, `ConflictExceptionHandler`→**409**, `GlobalExceptionHandler`→500); **o projeto não usa 422.**

| Método/rota | Ação |
|---|---|
| `GET /api/prompts/{promptId}/linked-documents` | Lista `LinkedDocumentDto[]` do prompt. |
| `POST /api/prompts/{promptId}/linked-documents` | Body `{ absolutePath, documentType?, displayName? }`. Valida arquivo, cria doc (Status=`Tracking`), lê conteúdo → **versão #1**, inicia watcher, broadcast `LinkedDocumentLinked`. Inválido (path/extensão/inexistente/dir/grande demais/ilegível) → **400** (`ValidationException`); duplicado `(PromptId,AbsolutePathKey)` → **409** (`ConflictException`). |
| `GET /api/linked-documents/{id}` | `LinkedDocumentDto` (404 se não existe). |
| `GET /api/linked-documents/{id}/content?version={n?}` | `LinkedDocumentContentDto` (última ou versão `n`). **Cacheável por versão** no front. |
| `GET /api/linked-documents/{id}/versions` | `LinkedDocumentVersionDto[]` (metadados). |
| `POST /api/linked-documents/{id}/pause` | Status=`Paused`, para watcher, broadcast `LinkedDocumentUpdated`. |
| `POST /api/linked-documents/{id}/resume` | Re-valida + re-lê (cria versão se mudou enquanto pausado), Status=`Tracking`, reinicia watcher, broadcast. |
| `POST /api/linked-documents/{id}/refresh` | Sync manual imediato (backstop p/ eventos perdidos). |
| `DELETE /api/linked-documents/{id}` | Para watcher, remove (cascade versões), broadcast `LinkedDocumentRemoved`. |

**Commands/Queries (MediatR, padrão "uma pasta por caso de uso"):** `Commands/{LinkDocument, PauseLinkedDocument, ResumeLinkedDocument, RefreshLinkedDocument, RemoveLinkedDocument}`, `Queries/{GetLinkedDocuments, GetLinkedDocument, GetLinkedDocumentContent, GetLinkedDocumentVersions}`. `LinkDocumentValidator`: `absolutePath` não vazio + `Path.IsPathFullyQualified` + extensão `.md`/`.markdown`. A validação forte do arquivo (existe/é arquivo/tamanho/legível) é no **handler** via o serviço de arquivo (como prompts validam menções no handler).

**Escopo por dono (OBRIGATÓRIO, cross-cutting):** `LinkedDocument` **não tem dono próprio** — o dono é o `Prompt` (`Prompt.OwnerId`, ver `Domain/Prompts/Prompt.cs`; `LinkedDocument` é `Entity`, não `AuditableEntity`). **Todo** handler (command **e** query) carrega o documento **junto do `Prompt`** e exige `prompt.OwnerId == currentUser.UserId` (via `ICurrentUser`); se falhar → `NotFoundException` (**404**, não revela existência). `GetLinkedDocuments` filtra por `promptId` **+** ownership do prompt; acesso por `linkedDocumentId` sempre faz `join → Prompt` antes de autorizar. No MVP single-user (user system) isso sempre passa, mas **fecha o vazamento p/ o futuro multiusuário** — sem isso a feature nasce insegura.

---

## 4. Fluxo de monitoramento de arquivo

### 4.1 Serviço de arquivo externo (novo) — `ILinkedDocumentFileService` (Application) + `Infrastructure/FileSystem/LinkedDocumentFileService.cs`
**Segurança deliberadamente diferente do workspace:** o arquivo **pode estar fora** de qualquer raiz → **não há containment**. Como é app **local-first de 1 usuário** (dono da máquina), ler arquivo local apontado é aceitável; ainda assim validar:
- `ValidateAsync(absolutePath)` → caminho **absoluto/fully-qualified**; extensão ∈ {`.md`,`.markdown`} (case-insensitive); `File.Exists` e **não** `Directory`; tamanho ≤ **cap** (`LinkedDocuments:MaxFileSizeBytes`, default 5 MB); legível (abrir com `FileShare.ReadWrite`). Retorna `{ IsValid, CanonicalPath, SizeBytes, Error }` (não lança — handler decide). Rejeitar UNC `\\` por padrão (config p/ liberar).
- `ReadAsync(absolutePath)` → lê texto UTF-8 (trata BOM), calcula **SHA-256 hex**, retorna `{ Success, Content, ContentHash, SizeBytes, Error }`. Trata `IOException`(lock)/`UnauthorizedAccess`/`FileNotFound` como `Error` (não lança no caminho do watcher).

### 4.2 Núcleo de sync (testável) — `LinkedDocumentSyncService` (Infrastructure, Scoped)
`Task<SyncOutcome> SyncAsync(Guid linkedDocumentId, ChangeSource source, CancellationToken ct)`:
1. Carrega `LinkedDocument` (+`Prompt` p/ obter `WorkingDirectoryId` do grupo). Se não existe/`Paused`/`Removed` → **no-op** (re-check de status evita corrida com comandos do usuário).
2. `ReadAsync`. Erro de leitura → Status=`Missing` (se sumiu) ou `Error` (ilegível), grava `LastError`, **mantém última versão**, `SaveChanges`, outcome=`Errored`.
3. Sucesso: compara `ContentHash` com `LastContentHash`.
   - **igual** → sem nova versão (**dedup, requisito 6**); atualiza só `LastSyncedAtUtc`/Status=`Tracking`/limpa `LastError`; outcome=`Unchanged`.
   - **diferente** → `CurrentVersion++`; cria `LinkedDocumentVersion` (snapshot, hash, size, `Source`); atualiza `LastContentHash`/`SizeBytes`/`LastSyncedAtUtc`/Status=`Tracking`/`LastError=null`; `SaveChanges`; outcome=`Updated`.
4. Retorna `SyncOutcome { LinkedDocumentDto, Changed, Status }`. **Não** chama SignalR (o chamador chama o notifier → testável sem hub).

### 4.3 Watcher — `Infrastructure/FileSystem/LinkedDocumentWatcherService.cs` : `BackgroundService` (Singleton) **e** `ILinkedDocumentWatchCoordinator`
- Coordinator (interface em Application): `Task StartTrackingAsync(Guid id, CancellationToken)`, `void StopTracking(Guid id)`. Comandos chamam **pós-commit**.
- Estado: `ConcurrentDictionary<string, DirectoryWatch>` **por diretório canônico** (lower no Windows). `DirectoryWatch` = `FileSystemWatcher` (no diretório, `NotifyFilter = LastWrite|FileName|Size`, `IncludeSubdirectories=false`, `InternalBufferSize` ampliado) + mapa `fileName → HashSet<Guid> docIds`. **Múltiplos docs no mesmo diretório compartilham 1 watcher** (requisito técnico 6).
- Eventos `Changed|Created|Renamed|Deleted` → resolve `fileName` → **debounce por (dir+arquivo)** (~400 ms, `LinkedDocuments:DebounceMilliseconds`) → enfileira `SyncAsync(docId, FileChanged)` num `Channel<Guid>` consumido por 1 worker (serializa trabalho de DB, evita thundering herd). **Hash-dedup** absorve eventos duplicados/spurious.
- **Padrão de escrita do Claude Code**: planos costumam ser regravados (write+rename atômico) → tratar `Created`/`Renamed` além de `Changed`; re-ler sempre e confiar no hash.
- **Startup** (`ExecuteAsync`): num scope, consulta `LinkedDocuments` com Status ∈ {`Tracking`,`Error`,`Missing`} e tenta registrar watchers; **não** bloqueia o boot (erros por-doc logados). `Paused`/`Draft` não são observados.
- **Diretório/arquivo inexistente** (no startup **ou** em `StartTracking`): o `FileSystemWatcher` **não consegue assinar um diretório que não existe** → marcar o doc **`Missing`** (com `LastError`), **sem watcher ativo**; a reativação fica por conta da reconciliação periódica e do `POST /refresh`. Tratar também o evento **`FileSystemWatcher.Error`** (diretório deletado em runtime, buffer estourado) marcando os docs daquele diretório como `Missing` e derrubando aquele `DirectoryWatch`.
- **Disposal correto** (`StopAsync`/`Dispose`): dispor todos os `FileSystemWatcher` + timers; completar o channel (requisito técnico 6).
- **Reconciliação periódica (recomendada — é o mecanismo de recuperação, não só otimização):** timer de baixa frequência (ex.: 30–60 s) que: (a) re-hasheia arquivos `Tracking` p/ recuperar eventos perdidos do FSW; (b) **re-tenta `Missing`/`Error`** — se o diretório/arquivo **reapareceu**, (re)assina o `FileSystemWatcher`, re-sincroniza e volta a `Tracking`. Complementada pelo `POST /refresh` manual.

### 4.4 DI
- Infrastructure: `AddScoped<ILinkedDocumentFileService, LinkedDocumentFileService>()`; `AddScoped<LinkedDocumentSyncService>()`; `AddSingleton<LinkedDocumentWatcherService>()`; `AddSingleton<ILinkedDocumentWatchCoordinator>(sp => sp.GetRequiredService<LinkedDocumentWatcherService>())`; `AddHostedService(sp => sp.GetRequiredService<LinkedDocumentWatcherService>())`. Watcher usa `IServiceScopeFactory` p/ DbContext por evento. `Configure<LinkedDocumentOptions>` (cap, debounce, allowUnc).
- Api: `AddScoped<ILinkedDocumentNotifier, SignalRLinkedDocumentNotifier>()`.

---

## 5. Eventos SignalR (separados de `PromptUpdated`)

Mesma conexão/hub (`/hubs/prompts`) e mesmo grupo `wd:{Prompt.WorkingDirectoryId}` (cliente já está no grupo na tela de detalhe). **Eventos novos e distintos** — não reutilizar `PromptUpdated`:
- Adicionar a `IPromptClient`: `Task LinkedDocumentLinked(LinkedDocumentDto doc)`, `Task LinkedDocumentUpdated(LinkedDocumentDto doc)`, `Task LinkedDocumentRemoved(Guid linkedDocumentId, Guid promptId, Guid workingDirectoryId)`.
- `ILinkedDocumentNotifier` (Application) + `Api/Realtime/SignalRLinkedDocumentNotifier.cs` (injeta `IHubContext<PromptHub, IPromptClient>`, broadcast em `PromptHub.GroupName(workingDirectoryId)`). Mantém o notifier de prompt intocado.
- **Payload é metadados (sem `Content`)**: `LinkedDocumentDto` carrega `CurrentVersion`/`Status`/`LastError`. O cliente **invalida/refetcha** a query de conteúdo (keyed por versão) → re-renderiza sem refresh manual e sem trafegar conteúdo grande no hub.
- **Quem notifica (regra fixa, sem ambiguidade):** o `LinkedDocumentSyncService` **NUNCA** chama SignalR — só persiste e retorna `SyncOutcome`. Quem chama `ILinkedDocumentNotifier` é o **handler** (link/pause/resume/remove) e o **watcher** (depois que `SyncAsync` retorna `Changed`), sempre **pós-commit**. Isso mantém o sync testável e evita notificação duplicada.

---

## 6. UI/UX proposta (frontend)

### 6.1 Camada de dados
- `src/api/linked-documents.ts`: `listForPrompt(promptId)`, `get(id)`, `getContent(id, version?)`, `listVersions(id)`, `link(promptId, body)`, `pause(id)`, `resume(id)`, `refresh(id)`, `remove(id)`.
- `src/api/schemas.ts`: `linkedDocumentStatusSchema` (`'Draft'|'Tracking'|'Paused'|'Error'|'Missing'`), `linkedDocumentTypeSchema`, `linkedDocumentSchema`, `linkedDocumentContentSchema`, `linkedDocumentVersionSchema` (+ list + `z.infer` types).
- `src/api/query-keys.ts`: `linkedDocuments: { forPrompt(promptId), detail(id), content(id, version), versions(id) }`.

### 6.2 Realtime (estender `src/realtime/prompt-hub.tsx`)
`connection.on('LinkedDocumentLinked'|'LinkedDocumentUpdated'|'LinkedDocumentRemoved', ...)` (validar payload com zod):
- `Linked` → `invalidateQueries(linkedDocuments.forPrompt(promptId))`.
- `Updated` → `setQueryData(linkedDocuments.detail(id))` + invalidate `forPrompt` + **invalidate `content(id)` e `versions(id)`** (versão mudou → refetch conteúdo).
- `Removed` → `removeQueries(detail/content/versions)` + invalidate `forPrompt`.
Reuso do join `wd:{id}` e do `onreconnected` já existentes.

### 6.3 Tela (detalhe do prompt)
Hoje `$promptId.tsx` = `[PromptForm | PromptVersions]`. **Recomendação:** introduzir **shadcn `Tabs`** no topo do detalhe: aba **"Prompt"** (form+versions atuais) e aba **"Plano vinculado"** (novo `LinkedDocumentsPanel`). Mantém a tela limpa e escala p/ mais tipos de documento. *(Alternativa mais simples: seção full-width abaixo do grid.)*

Componentes novos em `src/features/linked-documents/`:
- `LinkedDocumentsPanel.tsx` — lista dos docs do prompt (badge de status: Tracking/Paused/Error/Missing; nome/caminho com tooltip; última sync; versão atual; indicador `connected` do `usePromptHub`), botão **"Vincular plan markdown"**, estados loading/empty.
- `LinkDocumentForm.tsx` — input de **caminho absoluto** (RHF+zod, validação leniente no cliente; erro autoritativo do backend via `setError`), em `Dialog`. Sucesso → invalidate + toast.
- `LinkedDocumentViewer.tsx` — **markdown renderizado** (read-only) + ações **pausar/retomar/atualizar/remover** (remover com `AlertDialog` de confirmação), badge de status, aviso de erro (Missing/Error com `LastError`), em `ScrollArea`.
- `LinkedDocumentHistory.tsx` — lista de `versions` (número, data, tamanho); clicar carrega `getContent(id, version)` p/ ver versão anterior.

### 6.4 Renderização de markdown (nova dependência)
Adicionar (pin exato; `.npmrc save-exact` já ativo; rodar `npm audit`): **`react-markdown@10.1.0` + `remark-gfm@4.0.1` + `rehype-sanitize@6.0.0`**. Render read-only com GFM (tabelas/checkboxes comuns em planos) e **sanitize** (defesa em profundidade, conteúdo vem do disco). Estilizar no tema verde existente.

### 6.5 shadcn a adicionar
`npx shadcn@4.8.3 add dialog alert-dialog tabs card scroll-area tooltip` (badge/button/separator já existem). Atualiza `routeTree.gen.ts` não é necessário (sem rota nova; tudo dentro de `$promptId.tsx`).

---

## 7. Plano de implementação por etapas (cada etapa compila + testes verdes)

0. **Domínio + migration**: evoluir `LinkedDocument` (colunas/rename/nullable), enum `Status` (+Error/+Missing) e `LinkedDocumentType`; criar `LinkedDocumentVersion`(+`Source`)+config+DbSet+`IApplicationDbContext`+nav. `dotnet ef migrations add AddLinkedDocumentTracking` → `database update`. Sem comportamento.
1. **`ILinkedDocumentFileService`** + impl + **unit tests** (extensão/dir/missing/oversize/locked; read+hash+size). BCL puro.
2. **`LinkedDocumentSyncService`** (read→hash→dedup→versão→update) + **unit tests** (changed/unchanged/missing/error). Sem watcher.
3. **DTOs + `DtoMapper`** + **Commands/Queries** + **`LinkedDocumentsController`** + validators; coordinator como **stub no-op**. **Integração** dos endpoints (sem watcher).
4. **`ILinkedDocumentNotifier`** + `SignalRLinkedDocumentNotifier` + novos métodos em `IPromptClient`; ligar notifier **nos handlers e no watcher** (pós-commit) — **o `SyncAsync` não notifica**. **Integração**: eventos em link/pause/remove.
5. **`LinkedDocumentWatcherService`** (FSW + debounce + channel + coordinator + registro no startup + disposal) + backstop opcional. **Integração**: mudar arquivo real no disco → nova versão + `LinkedDocumentUpdated`.
6. **Frontend dados**: `api/linked-documents.ts`, schemas, query-keys, handlers no `prompt-hub.tsx`.
7. **Frontend UI**: `react-markdown`+gfm+sanitize, shadcn add, `LinkedDocumentsPanel`/`Form`/`Viewer`/`History`, Tabs no `$promptId.tsx`, estados loading/error/empty.
8. **E2E manual/browser** com um plano real (`C:\Users\psiel\.claude\plans\...md`): editar via Claude Code e ver atualizar; pausar/retomar/remover; apagar arquivo → Missing; histórico. Polir + doc.

---

## 8. Plano de testes

- **`Application.UnitTests`**: `LinkDocumentHandler` (válido → doc + versão #1 + hash + `StartTrackingAsync` chamado + notifier chamado; inválido → `ValidationException`; duplicado → `ConflictException`); `Pause/Resume/Remove` (transições de status + chamadas ao coordinator); `LinkedDocumentSyncService` (changed→nova versão; **unchanged→sem versão (dedup)**; missing/unreadable→Status `Missing`/`Error`+`LastError`, última versão preservada). Fakes p/ file service/notifier/coordinator/`IApplicationDbContext` (InMemory ou SQLite).
- **`Infrastructure.UnitTests`**: `LinkedDocumentFileService.ValidateAsync`/`ReadAsync` contra arquivos temporários (rejeita extensão/dir/missing/oversize/locked; aceita válido; hash determinístico). Se a lógica de **debounce/coalescing** for extraída p/ um componente com relógio injetável, testá-la isolada (o `FileSystemWatcher` cru é difícil de unitar).
- **`Api.IntegrationTests`** (Testcontainers PostgreSQL, já no projeto): endpoints link/get/content/versions/pause/resume/remove; **ProblemDetails** em caminho inválido; **409** em duplicado. **SignalR/watcher E2E**: criar `.md` temp, `POST link` → versão #1 + evento `LinkedDocumentLinked`; **alterar o arquivo no disco** → (com `TaskCompletionSource`+timeout) nova versão + `LinkedDocumentUpdated`; **apagar** → Status `Missing` + evento. Exercita o `FileSystemWatcher` real.
- **Frontend**: Vitest **não** está configurado hoje → recomendar como follow-up um teste de componente do `LinkedDocumentsPanel`; validação principal via manual/browser. (Opcional: configurar Vitest nesta feature.)
- **Manual/browser (Chrome DevTools/Playwright MCP)**: fluxo real com plano do Claude Code, observando atualização ao vivo, estados de erro e histórico.

---

## 9. Critérios de aceite

1. Consigo vincular 1..N arquivos `.md` (inclusive fora do workspace, ex.: `C:\Users\psiel\.claude\plans\x.md`) a um prompt; o conteúdo aparece renderizado na tela do prompt.
2. Caminho inválido (inexistente, diretório, extensão errada, grande demais, ilegível) → erro **claro** na UI (ProblemDetails) e nenhum vínculo criado.
3. Ao Claude Code **alterar** o `.md`, em segundos: backend detecta → lê → **cria nova versão** (com hash) → emite `LinkedDocumentUpdated` → a renderização atualiza **sem refresh manual**.
4. Conteúdo **idêntico** (mesmo hash) **não** gera versão duplicada.
5. Eventos rápidos/duplicados do FSW são **debounced** e não geram trabalho/versões redundantes.
6. Arquivo removido/movido/ilegível → status **Missing/Error** com mensagem; **última versão preservada**; ao reaparecer, volta a `Tracking` e versiona.
7. **Pausar** para de monitorar; **retomar** re-sincroniza (versiona se mudou enquanto pausado); **remover** apaga vínculo + versões e some da UI ao vivo.
8. Histórico lista as versões e permite ver o conteúdo de uma versão anterior.
9. Eventos de documento **não** disparam lógica de `PromptUpdated` (canais separados) e vice-versa.
10. Watchers são iniciados no startup p/ docs `Tracking` e **descartados corretamente** no shutdown; reinício da API restaura o monitoramento. **Se o diretório do plano não existir no startup**, o doc fica `Missing` (sem watcher) e a **reconciliação periódica** o reativa quando o diretório/arquivo reaparecer.
11. `dotnet test` (3 suítes) verde, incluindo o E2E de watcher+SignalR.

---

## 10. Riscos / observações

- **Leitura de arquivo arbitrário fora do workspace** = relaxamento **deliberado** do path-safety (a feature exige). Mitigações: extensão `.md`/`.markdown`, cap de tamanho, é-arquivo-não-dir, caminho absoluto canônico, `FileShare.ReadWrite`, **sanitize** na renderização, rejeitar UNC por padrão. Aceitável p/ app local-first de 1 usuário (dono da máquina). Quando houver multiusuário/hospedagem, **reavaliar** (vira superfície séria de path/SSRF).
- **Confiabilidade do `FileSystemWatcher`**: perde eventos sob carga/rede; editores salvam por **rename atômico** (Claude Code regrava o plano) → assinar `Changed|Created|Renamed|Deleted`, tratar o evento `Error` (buffer/dir deletado) e **confiar no hash**; `InternalBufferSize` ampliado; **reconciliação periódica** (re-hash de `Tracking` + re-assinatura de `Missing`/`Error` quando o diretório reaparece) + `POST /refresh` manual como recuperação de eventos perdidos. Não monitorar drives de rede.
- **Corrida watcher × comandos** (ex.: pausar durante processamento): `SyncAsync` **re-checa Status** dentro do scope e vira no-op se `Paused`/removido.
- **Crescimento do banco** (snapshot completo por versão): aceitável p/ planos; futuro pode limitar histórico/retenção ou guardar diff. `Content` em `text`.
- **Migration** faz `RenameColumn RelativePath→AbsolutePath` e torna `WorkingDirectoryId` nullable — seguro porque a feature está **sem dados/uso**; confirmar em DB real antes de aplicar.
- **Payload SignalR** é metadados (sem conteúdo) → cliente refetcha por versão; evita trafegar markdown grande e mantém o hub leve.
- **Startup não-bloqueante**: registrar watchers em background com tratamento de erro por-doc; falha de um doc não derruba a API.
- **Concorrência otimista**: `LinkedDocument` **não** usa `xmin` (diferente de `Prompt`); ações do usuário são transições simples e o watcher re-checa status — last-write-wins aceitável. Reavaliar se virar concorrente de verdade.
- **Sem novos pacotes NuGet** (FSW/SHA-256/BackgroundService são BCL). Frontend adiciona só `react-markdown`/`remark-gfm`/`rehype-sanitize` (pin exato + `npm audit`).
- **Caminhos Windows**: reusar conceito de canonicalização do `WorkspaceFileService` (`Path.GetFullPath` + resolver link → fallback) ao montar **`AbsolutePathKey`** (lowercase + separadores padronizados), que é a chave do índice único `(PromptId, AbsolutePathKey)` **e** a base do agrupamento de watcher por diretório (comparação `OrdinalIgnoreCase`). `AbsolutePath` real é preservado p/ exibir/abrir.

---

## Arquivos a criar/alterar (resumo)

**Backend — criar:** `Domain/Prompts/{LinkedDocumentVersion.cs, LinkedDocumentVersionSource.cs, LinkedDocumentType.cs}` · `Infrastructure/Persistence/Configurations/LinkedDocumentVersionConfiguration.cs` · `Infrastructure/Persistence/Migrations/*_AddLinkedDocumentTracking.cs` · `Application/Common/Interfaces/{ILinkedDocumentFileService.cs, ILinkedDocumentNotifier.cs, ILinkedDocumentWatchCoordinator.cs}` · `Application/Common/Models/{LinkedDocumentDto.cs, LinkedDocumentContentDto.cs, LinkedDocumentVersionDto.cs, MarkdownFileValidation.cs, MarkdownFileReadResult.cs}` · `Application/Features/LinkedDocuments/**` (commands/queries/validators) · `Infrastructure/FileSystem/{LinkedDocumentFileService.cs, LinkedDocumentSyncService.cs, LinkedDocumentWatcherService.cs}` · `Api/Controllers/LinkedDocumentsController.cs` · `Api/Realtime/SignalRLinkedDocumentNotifier.cs`.
**Backend — alterar:** `Domain/Prompts/{LinkedDocument.cs, LinkedDocumentStatus.cs}` · `Infrastructure/Persistence/Configurations/LinkedDocumentConfiguration.cs` · `Infrastructure/Persistence/ApplicationDbContext.cs` + `Application/Common/Interfaces/IApplicationDbContext.cs` · `Application/Common/Realtime/IPromptClient.cs` · `Application/Common/Mappings/DtoMapper.cs` · `Infrastructure/DependencyInjection.cs` · `Api/DependencyInjection.cs`.
**Frontend — criar:** `src/api/linked-documents.ts` · `src/features/linked-documents/{LinkedDocumentsPanel,LinkDocumentForm,LinkedDocumentViewer,LinkedDocumentHistory}.tsx` · componentes shadcn (dialog/alert-dialog/tabs/card/scroll-area/tooltip).
**Frontend — alterar:** `src/api/{schemas.ts, query-keys.ts}` · `src/realtime/prompt-hub.tsx` · `src/routes/workspaces/$workspaceId/prompts/$promptId.tsx` · `package.json` (markdown deps).
**Tests:** `Application.UnitTests/*LinkedDocument*` · `Infrastructure.UnitTests/LinkedDocumentFileServiceTests.cs` (+ debounce se extraído) · `Api.IntegrationTests/LinkedDocumentsFlowTests.cs` (endpoints + watcher/SignalR E2E).
