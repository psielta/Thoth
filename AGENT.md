# AGENT.md

Guia operacional para agentes de codigo trabalhando neste repositorio.

## Contexto do Projeto

Thoth e um gerenciador local-first de prompts em Markdown para Claude Code e Codex. O produto organiza prompts por diretorio de trabalho, valida mencoes de arquivos, vincula planos Markdown externos, versiona alteracoes e usa SignalR para manter o navegador atualizado em tempo real.

Trate este repositorio como projeto de portfolio: alteracoes devem preservar clareza arquitetural, consistencia visual e boa cobertura de validacao.

## Regras de Produto

- A listagem do workspace deve mostrar somente prompts pai.
- Prompts gerados a partir de planos vinculados sao prompts filhos do prompt que possui o plano.
- Prompts filhos devem ser exibidos na tab `Prompts filhos` do prompt pai.
- Clicar em um prompt filho deve abrir drawer na rota do prompt pai; nao redirecione para edicao do filho.
- O drawer de geracao de prompt filho deve ter apenas a acao de criar; nao reintroduza `Criar e abrir`.
- Referencias `@arquivo` precisam ser validadas contra o diretorio de trabalho no backend.
- Planos vinculados devem ser versionados e refletidos em tempo real.
- Arquivar um prompt deve pausar o monitoramento de planos vinculados.

## Arquitetura Backend

- Preserve Clean Architecture:
  - `Domain`: entidades, enums e conceitos de dominio.
  - `Application`: comandos, consultas, handlers MediatR, validadores, DTOs e contratos.
  - `Infrastructure`: EF Core, PostgreSQL, filesystem, cache e watchers.
  - `Api`: controllers, SignalR, OpenAPI, DI e configuracao HTTP.
- Nao coloque regra de negocio em controllers.
- Para novos casos de uso, prefira command/query MediatR com validator quando houver entrada externa.
- Para mudancas de schema, gere migration EF Core e atualize os testes.
- Se mudar DTOs do backend, atualize schemas Zod e tipos do frontend.
- Mantenha `RowVersion`/concorrencia em updates de prompt.

## Arquitetura Frontend

- Use TanStack Router para novas rotas.
- Use TanStack Query para chamadas remotas, cache e invalidacao.
- Valide payloads de API com Zod em `frontend/src/api/schemas.ts`.
- Centralize query keys em `frontend/src/api/query-keys.ts`.
- Mantenha funcionalidades agrupadas em `frontend/src/features`.
- Use componentes existentes em `frontend/src/components` antes de criar novos.
- Use lucide-react para icones.
- Para navegar/visualizar arquivos do workspace, reutilize o visualizador Monaco e a arvore em `frontend/src/features/files` (`FileExplorer`, `WorkspaceFileTree`, `FileViewerPanel`).
- Evite telas de marketing dentro do produto; o app deve abrir direto na experiencia funcional.

## Comandos de Desenvolvimento

Banco:

```powershell
docker compose up -d
```

API:

```powershell
dotnet run --project backend/src/Thoth.Api/Thoth.Api.csproj
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Producao:

```powershell
powershell -File scripts\db-bootstrap.ps1 -DbHost localhost -Port 5435 -Superuser postgres -AppUser prompttasks -AppPassword prompttasks -Database prompttasks
powershell -File build.ps1 -Bump patch
```

O instalador de producao roda como Windows Service `PromptTasks` na porta fixa `8091`. PostgreSQL deve ser instalado manualmente; EF cria/atualiza apenas o schema em um banco existente. O assistente tambem configura Agent Usage e preserva os caminhos existentes de Claude/Codex em upgrades.

## Validacao Esperada

Execute conforme o tipo de mudanca.

Backend:

```powershell
dotnet build backend/Thoth.sln
dotnet test backend/Thoth.sln
```

Frontend:

```powershell
cd frontend
npm run test
npm run lint
npm run build
```

Release:

```powershell
powershell -File build.ps1
```

Validar o instalador inclui `sc query PromptTasks`, `http://localhost:8091`, refresh de deep-link da SPA, `GET /api/ai/models` nao vazio e `GET /api/<rota-inexistente>` retornando 404 sem HTML.

Para mudancas visuais ou de fluxo, valide tambem no navegador. O frontend roda em `http://localhost:5190` e a API em `http://localhost:5191`.

## Observacoes de Ambiente

- Ambiente principal: Windows/PowerShell.
- Use `rg` para buscar arquivos e texto.
- Use migrations EF para alteracoes de banco.
- O build do frontend pode emitir avisos `INVALID_ANNOTATION` vindos de `@microsoft/signalr`; se o comando terminar com sucesso, eles sao nao bloqueantes.
- Nao versionar `node_modules`, `dist`, `build`, `backup`, artefatos temporarios de Playwright ou arquivos locais de banco.

## Padrao de Entrega

- Mantenha as alteracoes pequenas e coerentes com o pedido.
- Nao reverta trabalho existente que nao faz parte da sua mudanca.
- Atualize testes quando alterar comportamento.

## Commits

- Use sempre Conventional Commits, **sem** a linha `Co-Authored-By`.
- Faca commits separados por mudanca logica; evite um unico commit grande com mudancas nao relacionadas.
- Conforme terminar cada tarefa, crie o commit correspondente e faca push.
- Para fechar uma issue automaticamente pelo commit, use palavra-chave do GitHub em **ingles** no corpo da mensagem: `Closes #N`, `Fixes #N` ou `Resolves #N`. Termos em portugues como `Fecha #N` **nao** acionam o auto-close (viram apenas referencia); nesse caso, feche manualmente com `gh issue close N`. O fechamento via commit so vale quando ele chega ao branch padrao (`main`).
