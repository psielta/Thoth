# Plano — Gerenciador de Tarefas Definidas por Prompt (local-first)

## Contexto

Projeto **greenfield** (diretório `D:\gerenciamento-de-tarefas-definidas-por-prompt` vazio, sem git). O objetivo é um gerenciador de tarefas onde cada "tarefa" é um **prompt em Markdown** escrito para ser entregue ao **Claude Code** e ao **Codex**. O usuário:

1. Define um **diretório de trabalho** (caminho absoluto real na máquina dele).
2. Dentro desse diretório, escreve prompts em Markdown num editor rico com **autocomplete `@arquivo`** — ao digitar `@main` aparece a lista de arquivos do diretório (igual ao Claude Code/Codex).
3. Todo prompt é **persistido no PostgreSQL**.
4. Alterações de prompt são propagadas **em tempo real via SignalR**.

Decisões já confirmadas com o usuário:
- **Local-first**: o backend .NET roda na máquina do usuário (localhost) e lê o filesystem local diretamente. Postgres local via Docker.
- **Sem login no MVP**, mas o schema já tem `OwnerId` **NOT NULL** apontando p/ um usuário "system" semeado (multiusuário futuro sem migração dolorosa; NOT NULL é necessário p/ os índices únicos por dono funcionarem no Postgres).
- **Entrega desta fase**: scaffold Clean Architecture completo + fluxo principal funcionando ponta a ponta.
- **Editor**: TipTap (ProseMirror) com extensão de menção `@`.

**Extensibilidade futura (desenhar, não implementar agora):** num prompt de planejamento, após gerar o markdown do plano, um botão "amarrar ao markdown gerado para Claude Code" abre esse markdown em tela e o acompanha **ao vivo** (FileSystemWatcher → SignalR). O schema (`LinkedDocument`) e a infraestrutura de realtime/path-safety já ficam prontos para isso.

---

## Arquitetura geral (monorepo)

```
D:\gerenciamento-de-tarefas-definidas-por-prompt\
├─ backend/        # solução .NET 10 (Clean Architecture)
├─ frontend/       # app React + Vite + TS
├─ docker-compose.yml   # Postgres 18
└─ README.md
```

**Contrato compartilhado (canônico — usar exatamente estes nomes/portas nos dois lados):**
- API base: `http://localhost:5080/api`
- Hub SignalR: `http://localhost:5080/hubs/prompts`
- Front dev (Vite): `http://localhost:5173`
- CORS: backend libera `http://localhost:5173` com `AllowCredentials()` (SignalR não funciona com `AllowAnyOrigin` + credenciais).
- **Métodos do hub (client→server):** `JoinWorkingDirectory(Guid id)`, `LeaveWorkingDirectory(Guid id)`.
- **Eventos do hub (server→client):** `PromptCreated(PromptDto)`, `PromptUpdated(PromptDto)`, `PromptDeleted(Guid promptId, Guid workingDirectoryId)`. Grupo = `wd:{workingDirectoryId}`.
- **Busca de arquivo:** `GET /api/files/search?workingDirectoryId={guid}&query={txt}&limit={n}` → `[{ relativePath, fileName, isDirectory, score }]`. `relativePath` é sempre relativo à raiz, separador `/`, e é exatamente o `id` que vira a menção `@relativePath` no markdown.

> O termo de UI "workspace" no frontend é só açúcar para "working directory" do domínio.

---

## Versões fixadas (verificadas em 2026-05-30; re-confirmar no `install`)

**Backend (NuGet):** .NET SDK: instalado `10.0.106`; **recomendado atualizar p/ ≥`10.0.108`** (traz runtime `10.0.8`, casando com os pacotes abaixo). `global.json` fixa a feature band instalada (`10.0.106`) com `rollForward: latestFeature`. MediatR `14.1.0` · FluentValidation + `.DependencyInjectionExtensions` `12.1.1` · Npgsql.EntityFrameworkCore.PostgreSQL `10.0.2` · Microsoft.EntityFrameworkCore/.Relational/.Design `10.0.8` (fixar os três) · Microsoft.AspNetCore.Mvc.NewtonsoftJson `10.0.8` · Microsoft.AspNetCore.OpenApi `10.0.8` · Microsoft.Extensions.FileSystemGlobbing `10.0.8` · Microsoft.Extensions.Caching.Memory `10.0.8` (IMemoryCache na Infra) · Scalar.AspNetCore `2.14.14` (UI do OpenAPI) · `dotnet-ef 10.0.8` como **tool local**.

> Pacotes standalone (NewtonsoftJson, OpenApi, EF Core, Npgsql) podem ser referenciados em `10.0.8` mesmo no SDK `10.0.106`; SignalR vem do shared framework. Ainda assim, atualizar o SDK alinha runtime↔pacotes e evita skew. (Fonte: dotnet.microsoft.com/download/dotnet/10.0; nuget.org NewtonsoftJson.)

**Frontend (npm):** vite `8.0.14` (suportado: `@vitejs/plugin-react@6` exige vite `^8`; `@tanstack/router-plugin@1.168.13` aceita Vite 8; Node `v22.19.0` instalado satisfaz o mínimo do Vite 8) · @vitejs/plugin-react `6.0.2` · tailwindcss + @tailwindcss/vite `4.3.0` · shadcn CLI `4.8.3` · @tanstack/react-router `1.170.10` + @tanstack/router-plugin `1.168.13` + @tanstack/react-router-devtools `1.167.0` · @tanstack/react-query (+devtools) `5.100.14` · react-hook-form `7.76.1` · zod `4.4.3` · @hookform/resolvers `5.4.0` · @tiptap/{react,core,pm,starter-kit,extension-mention,suggestion,markdown} `3.23.6` + @floating-ui/dom `1.7.6` · @microsoft/signalr `10.0.0` · ky `2.0.2` · sonner `2.0.7`.

> **Pinning obrigatório:** criar `.npmrc` com `save-exact=true`; instalar **exatamente** as versões acima (`pkg@x.y.z`); **commitar `package-lock.json`** e rodar `npm audit` (resolver antes de prosseguir). Disciplina de lockfile + pin mitiga risco de cadeia transitiva — contexto do advisory recente do TanStack Router **GHSA-g7cv-rxg3-hmpx** (as versões acima não são as afetadas, mas ranges/transitivos sem lockfile são risco desnecessário).

Usar **Central Package Management** no backend (`Directory.Packages.props` + `Directory.Build.props` com `net10.0`, `Nullable enable`, `ImplicitUsings enable`).

---

## Backend — `backend/` (.NET 10, Clean Architecture)

### Projetos (dependências apontam para dentro: Api → Infrastructure → Application → Domain)

```
backend/
├─ PromptTasks.sln  global.json  Directory.Build.props  Directory.Packages.props
├─ .config/dotnet-tools.json
├─ src/
│  ├─ PromptTasks.Domain/          # sem deps NuGet
│  ├─ PromptTasks.Application/     # MediatR, FluentValidation (sem EF, sem ASP.NET)
│  ├─ PromptTasks.Infrastructure/  # EF Core/Npgsql, filesystem real
│  └─ PromptTasks.Api/             # Controllers + Newtonsoft, SignalR hub, composição
└─ tests/
   ├─ PromptTasks.Application.UnitTests/      # xUnit — handlers, validators, behaviors
   ├─ PromptTasks.Infrastructure.UnitTests/   # xUnit — WorkspaceFileService (path-safety)
   └─ PromptTasks.Api.IntegrationTests/       # xUnit + WebApplicationFactory + Testcontainers
```

**Decisão fixada:** usar **Controllers** (não Minimal APIs). Newtonsoft só integra com o pipeline MVC (`AddControllers().AddNewtonsoftJson(...)`); Minimal API ignora o formatter Newtonsoft. MediatR é agnóstico a transporte, então controllers ficam finos (`Mediator.Send(...)`).

### Domain — entidades (Guid PK via `Guid.CreateVersion7()` para chaves ordenadas)

| Entidade | Campos-chave |
|---|---|
| `User` | `Id`, `DisplayName`, `IsSystem`, `CreatedAtUtc`. MVP semeia 1 row "system" com Guid fixo `…0001` (FK determinística p/ `OwnerId`). |
| `WorkingDirectory` | `Id`, `Name`, `AbsolutePath` (canonicalizado), `OwnerId`→User (**NOT NULL**, = user system no MVP), `RespectGitignore` (default true), timestamps. Índice único `(OwnerId, AbsolutePath)` — **só funciona porque `OwnerId` é NOT NULL** (no Postgres NULLs são distintos e furariam a unicidade). |
| `Prompt` | `Id`, `WorkingDirectoryId`→WD (Cascade), `Title`, `Content` (text, markdown), `TargetAgent` (enum: ClaudeCode/Codex), `Kind` (General/Planning), `Status` (Draft/Ready/Archived), `OwnerId`→User (**NOT NULL**, = system), `CurrentVersion`, `xmin` (rowversion p/ concorrência otimista → 409; **exposto como `rowVersion` no `PromptDto` e exigido no PUT/PATCH**), timestamps. Índices `(WorkingDirectoryId, Status)`, `(WorkingDirectoryId, UpdatedAtUtc desc)`. |
| `PromptVersion` | Histórico append-only ("todo prompt fica gravado"): `Id`, `PromptId`→Prompt (Cascade), `VersionNumber`, snapshot de Title/Content/TargetAgent/Kind/Status, `ChangeNote?`, `CreatedAtUtc`. Único `(PromptId, VersionNumber)`. |
| `PromptFileReference` | Menções `@` por prompt (**populadas a partir do `mentions[]` enviado pelo cliente e validado no servidor — NÃO por regex solta no Markdown**): `Id`, `PromptId`→Prompt (Cascade), `RelativePath` (validado contra o WD), `RawMention`, `Exists` (checado via `IWorkspaceFileService`), `ResolvedAtUtc?`. Opcional: cruzar com o `Content` p/ consistência. |
| `LinkedDocument` (**stub futuro**) | `Id`, `PromptId`, `WorkingDirectoryId` (Restrict p/ evitar multiple-cascade-path), `RelativePath`, `Status`, `LastContentHash?`, timestamps. Tabela criada agora, watcher só no futuro. |

Enums via `.HasConversion<int>()`. `DateTimeOffset`→`timestamptz` (UTC). `AuditableEntityInterceptor` (SaveChangesInterceptor) carimba timestamps + `OwnerId`. Configs `IEntityTypeConfiguration<T>` por entidade + `ApplyConfigurationsFromAssembly`. `IDesignTimeDbContextFactory` para o `dotnet ef`.

### Application — MediatR + FluentValidation

- Convenção **uma pasta por caso de uso**: `Features/Prompts/Commands/CreatePrompt/` contém `CreatePromptCommand`, `CreatePromptHandler`, `CreatePromptValidator`, `CreatePromptResponse`. Escala bem conforme "features de manipulação crescem".
- Pipeline behaviors (ordem de registro, externo→interno): `UnhandledExceptionBehavior` → `LoggingBehavior` → `ValidationBehavior` (`IPipelineBehavior<,>`). `ValidationBehavior` injeta `IEnumerable<IValidator<TRequest>>`, agrega falhas, lança `ValidationException` da Application (sem conhecer HTTP).
- Interfaces na Application (impl na Infrastructure): `IApplicationDbContext`, `IWorkspaceFileService`, `IPromptNotifier`, `ICurrentUser`, `IDateTimeProvider`.
- Erros como **ProblemDetails (RFC 9457)** via `AddProblemDetails()` + cadeia de `IExceptionHandler` (`ValidationExceptionHandler`→400, `NotFoundExceptionHandler`→404, `PathTraversalExceptionHandler`→400, fallback→500).
- Newtonsoft: `ContractResolver = CamelCasePropertyNamesContractResolver`, `StringEnumConverter` (enums viajam como string p/ o React; DB guarda int). Manter camelCase também porque o OpenAPI nativo deriva schema do System.Text.Json (ver riscos).

### Infrastructure — busca de arquivo `@` (peça crítica de segurança)

`IWorkspaceFileService` (abstração na Application, impl real na Infrastructure):
- `ValidatePathAsync(absolutePath)` — usado no cadastro do WD.
- `SearchAsync(rootAbsolutePath, query, limit, ct)` — usado no autocomplete.

**Algoritmo de segurança (rejeitar, não "sanitizar e seguir"):**
1. Canonicalizar a raiz uma vez: `Path.GetFullPath`; depois `DirectoryInfo.ResolveLinkTarget(returnFinalTarget: true)` segue symlink/junction até o alvo final — **mas retorna `null` quando o caminho NÃO é link** (diretório normal), então usar `info.ResolveLinkTarget(true)?.FullName ?? Path.GetFullPath(path)` como caminho canônico. Aplicar o mesmo a cada candidato.
2. Para cada candidato, resolver ao alvo final e checar contenção com `Path.GetRelativePath(rootCanon, candCanon)` → rejeitar se resultado começa com `..` ou é `IsPathRooted`. Comparação `OrdinalIgnoreCase` no Windows. Acrescentar separador final à raiz antes de comparar prefixo (mata o bug `C:\work` × `C:\workspace`).
3. Resolver o alvo final **antes** da checagem de contenção derrota o escape por symlink (comparar o caminho lógico é o bypass clássico — não fazer).
4. **Nunca aceitar caminho absoluto do cliente** na busca: cliente manda `workingDirectoryId` (servidor resolve a raiz confiável) + `query` (filtro fuzzy, não um path para abrir). Violação → `PathTraversalException` → 400 + log de warning.

**Enumeração:** walk recursivo próprio (Queue/Stack) para **podar subárvore inteira por nome de diretório** no meio do caminho (`RecurseSubdirectories` não permite podar). `EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = ReparsePoint | System }`. Ignore-set fixo (case-insensitive): `node_modules .git .hg .svn bin obj .vs .idea dist build .next .venv __pycache__ target .gradle`. `.gitignore` de topo opcional via `Microsoft.Extensions.FileSystemGlobbing` (opt-in `RespectGitignore`).

**Match/ranking:** subsequência case-insensitive contra `relativePath` e `fileName` (boost em filename), bônus por run contíguo e início de segmento (estilo fzf), desempate por path mais curto, cap em `limit` (default 50, máx 200) via `PriorityQueue` limitada. Query vazia → primeiros N (mais raso → alfabético). Respeitar `CancellationToken`. Cache por `workingDirectoryId` em `IMemoryCache` (TTL ~8s) p/ reuso entre teclas.

### SignalR

- `PromptHub : Hub<IPromptClient>` em `/hubs/prompts`; contrato tipado `IPromptClient` (`PromptCreated/PromptUpdated/PromptDeleted` + futuro `LinkedDocumentChanged`).
- Grupos por WD: `JoinWorkingDirectory(id)` → `Groups.AddToGroupAsync(ConnectionId, $"wd:{id}")`; broadcasts só pro grupo.
- Publicação desacoplada via `IPromptNotifier` (impl `SignalRPromptNotifier` injeta `IHubContext<PromptHub, IPromptClient>`). Recomendado: **domain event → MediatR `INotification` → handler chama o notifier**, disparado **depois do commit** (no `SavedChangesAsync` do interceptor) — assim handlers de comando não conhecem realtime e novos side-effects são só novos notification handlers.
- Nota de Clean Architecture: hub fica na Api; `IPromptClient` na Application; `SignalRPromptNotifier` na Api (Api implementa `IPromptNotifier`) para evitar referência Infrastructure→Api. Documentar essa escolha.
- Futuro: `LinkedDocumentWatcherService : BackgroundService` com `FileSystemWatcher` por doc `Tracking`, debounce, re-hash, compara `LastContentHash`, atualiza e empurra `LinkedDocumentChanged`. Reusa path-safety + realtime já prontos.

### Endpoints REST (MVP) — base `/api`

- **Working Directories:** `GET /working-directories`, `GET /{id}`, `POST` (valida path), `PUT /{id}`, `DELETE /{id}` (cascade prompts), `POST /working-directories/validate-path`.
- **Prompts:** `GET /prompts?workingDirectoryId=&status=&agent=&kind=&q=`, `GET /{id}`, `POST` (body inclui `mentions[]`; cria Version #1; **valida cada `mentions[].relativePath` contra o WD** via `IWorkspaceFileService`; broadcast `PromptCreated`), `PUT /{id}` (**exige `rowVersion`**; snapshot nova versão; revalida `mentions[]`; broadcast `PromptUpdated`; conflito `xmin`→409), `PATCH /{id}/status` (exige `rowVersion`), `DELETE /{id}` (broadcast `PromptDeleted`), `GET /{id}/versions`. **`PromptDto` expõe `rowVersion`** (token de concorrência derivado do `xmin`).
- **Files:** `GET /files/search?workingDirectoryId=&query=&limit=`.
- **Docs (dev):** `GET /openapi/v1.json`, `GET /scalar`.

### Configuração

- `docker-compose.yml`: `postgres:18-alpine`, db/user/pass `prompttasks`, `5432:5432`, volume nomeado, healthcheck `pg_isready`.
- `appsettings.Development.json`: connection string + `Cors:AllowedOrigins=["http://localhost:5173"]`. Kestrel na porta **5080**.
- Migrations na Infrastructure; em `IsDevelopment()` → `db.Database.MigrateAsync()` + `DbSeeder` (user system) + `MapOpenApi()` + `MapScalarApiReference()`.
- Ordem do `Program.cs`: `AddApplication().AddInfrastructure(config).AddApiServices(config)` → build → `UseExceptionHandler()` → `UseCors("spa")` → `MapControllers()` → `MapHub<PromptHub>("/hubs/prompts")` → (dev) migrate/OpenAPI/Scalar → `Run()`.

---

## Frontend — `frontend/` (React + Vite + TS)

### Setup (ordem importa)

1. `npm create vite@9.0.7 . -- --template react-ts` (dentro de `frontend/`; scaffolder pinado — as deps do template são normalizadas logo abaixo pelos installs exatos). **Criar `.npmrc` com `save-exact=true` antes dos installs.** Fixar o toolchain: `npm i -D vite@8.0.14 @vitejs/plugin-react@6.0.2`.
2. Tailwind v4: `npm i tailwindcss@4.3.0 @tailwindcss/vite@4.3.0`; `src/index.css` = `@import "tailwindcss";` (sem `tailwind.config.js`, sem PostCSS).
3. Router: `npm i @tanstack/react-router@1.170.10` + `-D @tanstack/router-plugin@1.168.13 @tanstack/react-router-devtools@1.167.0`.
4. Query: `npm i @tanstack/react-query@5.100.14` + `-D @tanstack/react-query-devtools@5.100.14`.
5. Forms: `npm i react-hook-form@7.76.1 zod@4.4.3 @hookform/resolvers@5.4.0`.
6. Editor: `npm i @tiptap/react@3.23.6 @tiptap/core@3.23.6 @tiptap/pm@3.23.6 @tiptap/starter-kit@3.23.6 @tiptap/extension-mention@3.23.6 @tiptap/suggestion@3.23.6 @tiptap/markdown@3.23.6 @floating-ui/dom@1.7.6`.
7. SignalR + HTTP: `npm i @microsoft/signalr@10.0.0 ky@2.0.2 sonner@2.0.7`.
8. `vite.config.ts`: plugins **`tanstackRouter({ target:'react', autoCodeSplitting:true })` ANTES de `react()`**, depois `tailwindcss()`; alias `@`→`./src`.
9. Path alias `@/*` em `tsconfig.json` **e** `tsconfig.app.json`.
10. `npx shadcn@4.8.3 init` (CSS = `src/index.css`, CSS variables = sim) → escreve `components.json`, `lib/utils.ts`.
11. `npx shadcn@4.8.3 add button input textarea label form dialog sonner dropdown-menu command popover tabs scroll-area resizable separator card badge skeleton tooltip select sheet`.
12. **Lockfile + auditoria:** commitar `package-lock.json`; rodar `npm audit` e resolver achados antes de codar (ver GHSA-g7cv-rxg3-hmpx).

### Estrutura

```
src/
  main.tsx            # QueryClientProvider + SignalRProvider + RouterProvider + <Toaster/>
  routeTree.gen.ts    # gerado pelo plugin (não editar)
  routes/             # file-based
    __root.tsx        # createRootRouteWithContext<{queryClient}>; AppShell + Outlet + devtools
    index.tsx         # "/" → redirect /workspaces
    workspaces/
      index.tsx                       # lista + criar diretório de trabalho
      $workspaceId/
        route.tsx                     # layout: carrega WD + entra no grupo SignalR
        index.tsx                     # lista de prompts
        prompts/new.tsx
        prompts/$promptId.tsx         # ver/editar
  components/
    ui/               # primitivos shadcn
    layout/           # AppShell, WorkspaceSwitcher
    editor/           # PromptEditor.tsx, MentionList.tsx, extensions/{FileMention.ts, createFileMentionSuggestion.tsx}
  features/{working-directories,prompts,file-search}/   # api.ts, queries.ts, schemas.ts, components/
  lib/
    api/client.ts                     # ky com prefixUrl + AbortSignal
    query/{query-client.ts, query-keys.ts}
    signalr/{connection.ts, events.ts, SignalRProvider.tsx, use-signalr-cache-sync.ts}
    env.ts                            # VITE_API_BASE_URL, VITE_HUB_URL validados via Zod no boot
    markdown/mention-markdown.ts
  hooks/use-debounced-value.ts
```

### Data layer

- **HTTP**: `ky` (`prefixUrl: env.apiBaseUrl`, `AbortSignal` p/ cancelar busca `@`).
- **Zod** valida respostas (`.parse()` dentro da fn de api → cache só guarda dado validado, tipos via `z.infer`) e dirige forms (`zodResolver`).
- **Query keys** centralizadas (`lib/query/query-keys.ts`) — fonte única usada tanto pelos hooks quanto pelos handlers SignalR. Ex.: `prompts.list(wdId)`, `prompts.detail(wdId, id)`, `files.search(wdId, q)`.
- **queryOptions** compartilhado entre loader (`ensureQueryData`) e componente (`useSuspenseQuery`). Padrão Router↔Query atual: loader pré-carrega, componente lê do cache; como o SignalR escreve no mesmo cache, componentes com `useSuspenseQuery` re-renderizam ao vivo sem envolver o router.
- Busca de arquivo: hook keyed por `(wdId, debouncedQuery)`, `enabled: q.length>0` (ou top files no `@` puro), `placeholderData: keepPreviousData`, `signal` repassado ao `ky`.

### Editor TipTap `@` (centro do produto)

- `PromptEditor.tsx` é **componente controlado** expondo `{ value:{markdown, mentions}, onChange }`, consumido pelo RHF via `<Controller>`. Em cada `onUpdate`: `editor.getMarkdown()` + walk dos nós `mention` → monta `mentions[]` → `onChange` (debounce ~200ms).
- `FileMention`: **node customizado estendendo `@tiptap/extension-mention`** (`Mention.extend({...}).configure({ char:'@', suggestion: createFileMentionSuggestion(wdId) })`). ⚠️ **O `@tiptap/extension-mention@3.23.6` NÃO tem opção `renderMarkdown`** — só `renderText`, `renderHTML` e `suggestion`. A serialização em Markdown vem ao definir `renderMarkdown`/`parseMarkdown` **no node customizado** (mecanismo documentado pelo `@tiptap/markdown`) — `renderMarkdown` emite `@<id>`; **não** é uma opção de `configure()` da extensão stock. **Confirmar a API exata na implementação.** **Fallback garantido p/ o MVP:** configurar `renderText`/`renderHTML` para o texto literal `@<relativePath>`, de modo que o markdown exportado já contenha `@<path>` — sem depender de re-hidratar chips (usamos o `mentions[]` salvo à parte). Convenção de attrs: **`id` = path relativo à raiz do WD** (salvo), `label` = exibição.
- **Popup (TipTap v3 = Floating UI, NÃO tippy):** `createFileMentionSuggestion` usa `ReactRenderer` p/ a lista React + `@floating-ui/dom` (`computePosition` + `autoUpdate`). `items({query})` chama `fileSearchApi.search(wdId, query)` com debounce (~150–200ms) + `AbortController` que cancela a requisição anterior. **Limpar tudo no `onExit`** (`cleanup()` do autoUpdate + `component.destroy()` + remover o elemento flutuante) — há issue conhecida de leak do `ReactRenderer`; tratar também o double-invoke do StrictMode.
- `MentionList.tsx`: `forwardRef` + `useImperativeHandle` expondo `onKeyDown` (ArrowUp/Down/Enter); visual com `Command`/`CommandItem`; estados loading/empty; ao selecionar chama `props.command({ id, label })`.
- **Markdown:** usar o **oficial `@tiptap/markdown`** (não o community `tiptap-markdown`). Export: `editor.getMarkdown()` (a menção vira `@src/main.ts` pelo serializer do node `FileMention` — ver acima). Import (edição): `setContent(md, { contentType:'markdown' })`. **MVP persiste markdown E `mentions[]` estruturado**, então o app nunca precisa re-parsear `@` na volta (adicionar `parseMarkdown` só se quisermos re-hidratar os chips ao editar).
- Payload salvo: `{ title, markdown, mentions:[{id,label}] }`. `markdown` é a fonte da verdade entregue ao Claude/Codex; `mentions[]` é índice denormalizado p/ resolver arquivos e para a feature futura de "amarrar arquivo".

### SignalR (cliente)

- `connection.ts`: `HubConnectionBuilder().withUrl(env.hubUrl).withAutomaticReconnect([0,2000,5000,10000,30000]).build()`.
- `SignalRProvider.tsx`: UMA conexão no nível do app, `start()` no mount / `stop()` no unmount, StrictMode-safe (guardar `startPromise`). Expõe `joinWorkingDirectory(id)`/`leaveWorkingDirectory(id)`.
- Entrada no grupo em `workspaces/$workspaceId/route.tsx` (effect: join no mount/troca de id, leave no cleanup). No `onreconnected`, **re-invocar `JoinWorkingDirectory`** (grupos caem no reconnect) + `invalidateQueries(prompts.all)` p/ recuperar eventos perdidos.
- `use-signalr-cache-sync.ts`: traduz eventos → cache usando as MESMAS query keys. `PromptCreated/Updated` → `setQueryData(detail)` + `invalidateQueries(list)`; `PromptDeleted` → `removeQueries(detail)` + `invalidateQueries(list)`. Escritas idempotentes + dedupe por `id` p/ evitar flicker entre o eco do SignalR e o resultado da mutation.

### Forms (RHF + Zod + shadcn)

- **Criar diretório de trabalho:** `Form` + `Input` em `Dialog`. Zod `{ name:min(1), path:absoluto }` (regex lenient Windows/POSIX; checagem autoritativa de "existe/é dir" no backend, erro via `setError`). Sucesso → `invalidateQueries(workingDirectories.list())` + toast + (opcional) navegar pro WD.
- **Criar/editar prompt:** `title` (`Input`) + editor como campo controlado (`<Controller name="editor" render={({field}) => <PromptEditor value={field.value} onChange={field.onChange}/>}/>`). No submit, espalha `editor.markdown` + `editor.mentions` no payload.

---

## Ordem de implementação sugerida

1. **Monorepo + Docker**: criar `backend/`, `frontend/`, `docker-compose.yml`; `docker compose up -d`.
2. **Backend scaffold**: solução + 4 projetos + **3 projetos de teste** (`Application.UnitTests`, `Infrastructure.UnitTests`, `Api.IntegrationTests`) + references + Central Package Management + `global.json` + tool `dotnet-ef`. Tornar `Program` parcialmente público (`public partial class Program {}`) p/ os testes de integração.
3. **Domain + EF**: entidades, configs, `ApplicationDbContext`, interceptor, factory design-time, `InitialCreate`, `database update`, seeder do user system.
4. **Application**: behaviors (validation/logging/unhandled), exception handlers/ProblemDetails, features Working Directories + Prompts (commands/queries/validators/DTOs).
5. **Infrastructure**: `WorkspaceFileService` (path-safety + enumeração + fuzzy + cache) — testar com pasta real e tentativas de traversal.
6. **Api**: controllers finos, Newtonsoft, CORS, OpenAPI/Scalar, `PromptHub` + `SignalRPromptNotifier`, fluxo de domain event → notifier pós-commit. Subir e validar no Scalar. Em seguida, **escrever as 3 suítes de teste** (ver *Estratégia de testes*) e deixar `dotnet test` verde antes de ir pro frontend.
7. **Frontend scaffold**: Vite + Tailwind v4 + shadcn + Router + Query + alias; confirmar `routeTree.gen.ts` e um `button` estilizado.
8. **Frontend `lib/`**: env, ky client, query client, query keys, providers no `main.tsx`.
9. **Working Directories ponta a ponta** (sem SignalR): rotas, lista, dialog/form de criação.
10. **Prompts**: lista + detalhe com loader/`useSuspenseQuery`; render markdown estático.
11. **PromptEditor** sem menção (TipTap + Markdown) → validar round-trip export/import.
12. **Menção `@`**: `FileMention` + suggestion + Floating UI + `file-search` (debounce/abort) — o centro.
13. **Form de criar/editar prompt** com editor controlado; persistir `{markdown, mentions}`.
14. **SignalR**: provider, join no layout do workspace, `use-signalr-cache-sync`, reconnect.
15. **Polish**: toasts, skeletons, error boundaries, empty states. **Deixar só esqueleto** (rota + `resizable` split) da feature futura "amarrar markdown gerado".

---

## Riscos e decisões a registrar

- **Licença do MediatR (mantido por pedido explícito):** v13+ e a `14.1.0` são dual-license RPL-1.5 **ou** comercial. Tier **Community grátis** cobre educação/ONG/empresas < US$5M de receita — um projeto acadêmico (`@unifacvest.edu.br`) se enquadra; é preciso aceitar os termos. Mitigação de saída: handlers são `IRequestHandler<,>` e controllers usam só `ISender`/`IPublisher`, então trocar por um mediator MIT depois é quase sem custo.
- **Newtonsoft força Controllers** (Minimal API ignora o formatter). Decisão fixada.
- **OpenAPI nativo usa System.Text.Json p/ schema mesmo com Newtonsoft** (aspnetcore#60458, "by design"). Mitigar: DTOs POCO simples, camelCase nos dois serializadores, sem `[JsonProperty]` renomeando. OpenAPI aqui é só auxílio local.
- **EF Core 10 + Npgsql 10.0.2**: fixar EF Core/.Relational/.Design em `10.0.8` (Design explícito na Infrastructure; tools 10.0.6+ não puxam mais Design transitivo).
- **Path traversal** é o ponto mais sensível — resolver alvo final (symlink/junction) **antes** da contenção; rejeitar, nunca sanitizar; `OrdinalIgnoreCase` no Windows; cuidado com UNC/long paths/junctions em `AppData`.
- **Tailwind v4 + shadcn**: caminho só via `@tailwindcss/vite` (sem `postcss.config.js` nem `tailwind.config.js`); diretivas v3 (`@tailwind base`) quebram silenciosamente.
- **TanStack Router codegen**: `tanstackRouter()` antes de `react()`; `routeTree.gen.ts` é gerado (rodar dev uma vez antes do TS resolver tipos); devtools é `@tanstack/react-router-devtools`.
- **TipTap v3**: tippy removido → Floating UI; usar `@tiptap/markdown` oficial. **`extension-mention` não expõe `renderMarkdown`** → criar node custom (`Mention.extend`) com serializer markdown próprio, ou usar o fallback de texto literal `@<path>`; validar a API do serializer na implementação. Menção não re-hidrata de `@path` sem `parseMarkdown` (por isso o dual-store); testar round-trip de code fences/listas.
- **SignalR**: grupos caem no reconnect → re-`JoinWorkingDirectory` + invalidate; CORS precisa origem explícita + `AllowCredentials`; major do client (`@microsoft/signalr` 10) casando com .NET 10.
- **Supply chain (frontend)**: Vite 8 é suportado pelos plugins (`plugin-react@6` exige vite `^8`). Controlar risco com `save-exact` + `package-lock.json` commitado + `npm audit`; o advisory recente do TanStack Router **GHSA-g7cv-rxg3-hmpx** reforça a disciplina de lockfile (as versões fixadas não são as afetadas).
- **Confiança no Markdown**: backend **não** faz regex solta no conteúdo; recebe `mentions[]` e valida cada `relativePath` contra o WD (containment + existência) via `IWorkspaceFileService`, cruzando opcionalmente com o `Content`.
- **Concorrência otimista**: `xmin` exposto como `rowVersion` no `PromptDto` e **exigido** em `PUT`/`PATCH`; ausência ou conflito → 409 ProblemDetails. Histórico em `PromptVersion` facilita recuperação.
- **`OwnerId` NOT NULL**: necessário p/ os índices únicos por dono (Postgres trata NULL como distinto). MVP usa sempre o user system semeado.

---

## Estratégia de testes (unit + integração)

Três projetos sob `backend/tests/`, todos xUnit (`xunit 2.9.3` + `xunit.runner.visualstudio 3.1.5`), rodados com `dotnet test`.

**1. `PromptTasks.Application.UnitTests`** (sem I/O). Valida handlers, validators FluentValidation e os pipeline behaviors com `IApplicationDbContext` mockado (EF Core InMemory ou SQLite in-memory), `IWorkspaceFileService`/`IPromptNotifier`/`ICurrentUser`/`IDateTimeProvider` fakes. Casos: `CreatePrompt` cria `Prompt` + `PromptVersion #1`; `UpdatePrompt` incrementa versão + grava snapshot; `ValidationBehavior` agrega falhas e lança `ValidationException`; mapeamento de `mentions[]` → `PromptFileReference`; `IPromptNotifier` é chamado **após** o commit.

**2. `PromptTasks.Infrastructure.UnitTests`** (filesystem real temporário) — foco no `WorkspaceFileService`, a peça de segurança. No setup, montar uma árvore em `Path.GetTempPath()` com subpastas, arquivos, um `node_modules/` (deve ser podado) e — onde o SO permitir — um **symlink/junction apontando p/ fora da raiz**. Asserts:
- busca retorna só paths internos, `relativePath` com `/`, ranking/cap/empty-query corretos;
- `..`, caminho absoluto e symlink/junction que escapa da raiz → `PathTraversalException`;
- ignore-set podado; `CancellationToken` interrompe a enumeração;
- `ValidatePathAsync` rejeita arquivo/inexistente e canonicaliza.
- (Windows: criar junction via `mklink /J` no setup; pular o teste de symlink com `Skip` se faltar privilégio.)

**3. `PromptTasks.Api.IntegrationTests`** — `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) + **`Testcontainers.PostgreSql`** (PostgreSQL real efêmero **por suíte**), aplicando `Database.MigrateAsync()` + seed do user system no setup. Pacotes (pin): `Microsoft.AspNetCore.Mvc.Testing 10.0.8`, `Testcontainers.PostgreSql 4.12.0`, `Microsoft.AspNetCore.SignalR.Client 10.0.8`, `FluentAssertions` (opcional). A factory sobrescreve a connection string p/ o container. Cobrir:
- **Working Directories**: POST (valida uma pasta temp real criada no teste), GET/list, DELETE cascade, `validate-path`.
- **File search**: `GET /files/search` contra a pasta temp registrada — ranking e segurança (traversal → 400 ProblemDetails).
- **Prompts CRUD**: POST (Version #1 + `PromptFileReference` a partir de `mentions[]`), GET/list/filtros, PUT (nova versão), `GET /{id}/versions`, DELETE.
- **Concorrência `xmin`**: dois PUTs com o mesmo `rowVersion` → o segundo retorna **409** ProblemDetails.
- **ProblemDetails (RFC 9457)**: payload inválido → 400/422 com erros de validação; id inexistente → 404.
- **SignalR**: um `HubConnection` conecta em `/hubs/prompts`, faz `JoinWorkingDirectory`, e ao criar/editar/excluir prompt via REST recebe `PromptCreated/PromptUpdated/PromptDeleted` no grupo `wd:{id}` (assert via `TaskCompletionSource` + timeout).
- **Fluxo completo**: criar WD → criar prompt com menções → conferir `Prompt` + `PromptVersion #1` + `PromptFileReference` no banco → editar → `PromptVersion #2` + broadcast.

> Docker é pré-requisito da suíte de integração (Testcontainers reusa o Docker já exigido pelo Postgres de dev). Frontend: além de `npm audit`, deixar espaço p/ testes de componente do editor (Vitest) numa fase seguinte — fora do MVP.

## Verificação (end-to-end)

1. **Infra**: `docker compose up -d`; `dotnet ef database update` aplica `InitialCreate`; subir a Api (`dotnet run --project backend/src/PromptTasks.Api`), abrir `http://localhost:5080/scalar` e exercitar os endpoints.
2. **Testes automatizados**: `dotnet test` (com Docker ligado) roda as 3 suítes — unit (handlers/validators/behaviors), Infrastructure (path-safety / symlink / junction no `WorkspaceFileService`) e integração (endpoints + 409 + ProblemDetails + broadcast SignalR contra Postgres real via Testcontainers). Detalhe na seção *Estratégia de testes*.
3. **Fluxo do produto** (front em `http://localhost:5173`, `npm run dev`):
   - Criar um diretório de trabalho apontando p/ um repo real local; ver na lista.
   - Novo prompt: digitar `@` + parte de um nome → popup mostra arquivos reais; selecionar insere a menção; salvar.
   - Confirmar no banco (`Prompt`, `PromptVersion #1`, `PromptFileReference`) que o markdown e as menções foram gravados.
   - Editar o prompt em **outra aba/janela** apontando pro mesmo WD e confirmar que a lista/detalhe atualiza **ao vivo** (SignalR `PromptUpdated`), e que nova `PromptVersion` é criada.
   - Forçar conflito de edição concorrente e confirmar 409.
4. **Reconnect**: parar/subir a Api com o front aberto; confirmar reconexão automática, re-join do grupo e backfill via invalidate.

---

## Arquivos críticos a criar

- `backend/src/PromptTasks.Infrastructure/FileSystem/WorkspaceFileService.cs` — path-safety + busca `@` (núcleo de segurança).
- `backend/src/PromptTasks.Application/Common/Behaviors/ValidationBehavior.cs` — pipeline MediatR + FluentValidation.
- `backend/src/PromptTasks.Infrastructure/Persistence/ApplicationDbContext.cs` — contexto EF + configs.
- `backend/src/PromptTasks.Api/Program.cs` — composição (Newtonsoft, CORS, OpenAPI, ProblemDetails, SignalR, migrate dev).
- `backend/src/PromptTasks.Api/Hubs/PromptHub.cs` — hub + `IPromptClient` + grupos `wd:{id}`.
- `frontend/vite.config.ts` — ordem dos plugins (router → react → tailwind) + alias.
- `frontend/src/components/editor/extensions/createFileMentionSuggestion.tsx` — suggestion `@` + Floating UI.
- `frontend/src/components/editor/PromptEditor.tsx` — editor controlado p/ RHF.
- `frontend/src/lib/signalr/SignalRProvider.tsx` + `use-signalr-cache-sync.ts` — realtime → cache.
- `frontend/src/lib/query/query-keys.ts` — fonte única de chaves (hooks + SignalR).
- `backend/tests/PromptTasks.Infrastructure.UnitTests/WorkspaceFileServiceTests.cs` — testes de path traversal / symlink / junction.
- `backend/tests/PromptTasks.Api.IntegrationTests/` — `WebApplicationFactory` + Testcontainers (endpoints, 409, ProblemDetails, broadcast SignalR).
