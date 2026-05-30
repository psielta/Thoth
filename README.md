# Prompt Tasks

Local-first task manager for Markdown prompts aimed at Claude Code and Codex.

## Stack

- Backend: ASP.NET Core .NET 10, MediatR, FluentValidation, Newtonsoft.Json, EF Core, PostgreSQL, SignalR.
- Frontend: React, Vite, TypeScript, TanStack Query, TanStack Router, React Hook Form, Zod, shadcn/ui, Tailwind CSS.

## Development

```powershell
docker compose up -d
dotnet run --project backend/src/PromptTasks.Api/PromptTasks.Api.csproj --launch-profile http
cd frontend
npm run dev
```

API: `http://localhost:5080/api`

SignalR hub: `http://localhost:5080/hubs/prompts`

Scalar/OpenAPI: `http://localhost:5080/scalar`

Frontend: `http://localhost:5173`

## Validation

```powershell
dotnet test backend/PromptTasks.sln
cd frontend
npm run lint
npm run build
npm audit --audit-level=moderate
```
