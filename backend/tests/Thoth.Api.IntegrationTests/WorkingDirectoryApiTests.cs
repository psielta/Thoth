using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thoth.Application.Common.Models;
using Thoth.Domain.Ai;
using Thoth.Domain.Diagrams;
using Thoth.Domain.FutureTasks;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Infrastructure.Persistence;

namespace Thoth.Api.IntegrationTests;

public sealed class WorkingDirectoryApiTests(ThothApiFactory factory) : IClassFixture<ThothApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"prompttasks-workspace-delete-{Guid.NewGuid():N}");

    [Fact]
    public async Task Delete_workspace_removes_database_records_bound_to_it()
    {
        Directory.CreateDirectory(_tempRoot);
        var readmePath = Path.Combine(_tempRoot, "README.md");
        var planPath = Path.Combine(_tempRoot, "plan.md");
        await File.WriteAllTextAsync(readmePath, "# Repo");
        await File.WriteAllTextAsync(planPath, "# Plano");

        var client = factory.CreateClient();
        var workspace = await CreateWorkspaceAsync(client);
        var prompt = await CreatePromptAsync(client, workspace.Id);
        var linkedDocument = await LinkDocumentAsync(client, prompt.Id, planPath);
        var futureTask = await CreateFutureTaskAsync(client, workspace.Id);
        var diagram = await CreateDiagramAsync(client, workspace.Id);
        var notebook = await CreateNotebookAsync(client, workspace.Id);
        var note = await CreateNoteAsync(client, notebook.Id);

        var chatSessionId = Guid.CreateVersion7();
        var chatMessageId = Guid.CreateVersion7();
        var sequenceId = Guid.CreateVersion7();
        var workflowId = Guid.Empty;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            workflowId = await db.PromptWorkflows
                .Where(workflow => workflow.PromptId == prompt.Id)
                .Select(workflow => workflow.Id)
                .SingleOrDefaultAsync();

            var now = factory.Clock.UtcNow;
            db.AiChatSessions.Add(new AiChatSession
            {
                Id = chatSessionId,
                OwnerId = User.SystemUserId,
                WorkingDirectoryId = workspace.Id,
                PromptId = prompt.Id,
                Title = "Sessao",
                Model = "gemini-test",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            db.AiChatMessages.Add(new AiChatMessage
            {
                Id = chatMessageId,
                SessionId = chatSessionId,
                Role = "user",
                Content = "Oi",
                Sequence = 1,
                CreatedAtUtc = now
            });
            db.DailyTaskSequences.Add(new DailyTaskSequence
            {
                Id = sequenceId,
                WorkingDirectoryId = workspace.Id,
                SequenceDate = DateOnly.FromDateTime(now.UtcDateTime),
                CurrentValue = 7,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var response = await client.DeleteAsync($"/api/working-directories/{workspace.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await client.GetAsync($"/api/working-directories/{workspace.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.WorkingDirectories.Should().NotContain(item => item.Id == workspace.Id);
            db.Prompts.Should().NotContain(item => item.Id == prompt.Id);
            db.PromptVersions.Should().NotContain(item => item.PromptId == prompt.Id);
            db.PromptFileReferences.Should().NotContain(item => item.PromptId == prompt.Id);
            db.LinkedDocuments.Should().NotContain(item => item.Id == linkedDocument.Id);
            db.LinkedDocumentVersions.Should().NotContain(item => item.LinkedDocumentId == linkedDocument.Id);
            db.FutureTasks.Should().NotContain(item => item.Id == futureTask.Id);
            db.FutureTaskLabels.Should().NotContain(item => item.FutureTaskId == futureTask.Id);
            db.Diagrams.Should().NotContain(item => item.Id == diagram.Id);
            db.Notebooks.Should().NotContain(item => item.Id == notebook.Id);
            db.Notes.Should().NotContain(item => item.Id == note.Id);
            db.AiChatSessions.Should().NotContain(item => item.Id == chatSessionId);
            db.AiChatMessages.Should().NotContain(item => item.Id == chatMessageId);
            db.DailyTaskSequences.Should().NotContain(item => item.Id == sequenceId);
            if (workflowId != Guid.Empty)
            {
                db.PromptWorkflows.Should().NotContain(item => item.Id == workflowId);
                db.PromptWorkflowPhases.Should().NotContain(item => item.PromptWorkflowId == workflowId);
                db.PromptWorkflowEvents.Should().NotContain(item => item.PromptWorkflowId == workflowId);
            }
        }
    }

    private async Task<WorkingDirectoryDto> CreateWorkspaceAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/working-directories",
            new { name = "repo", absolutePath = _tempRoot, respectGitignore = true },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<WorkingDirectoryDto>(JsonOptions))!;
    }

    private static async Task<PromptDto> CreatePromptAsync(HttpClient client, Guid workingDirectoryId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/prompts",
            new
            {
                workingDirectoryId,
                title = "Implementar",
                content = "Leia @README.md",
                targetAgent = TargetAgent.Codex,
                kind = PromptKind.General,
                status = PromptStatus.Draft,
                mentions = new[] { new { id = "README.md", label = "README.md" } }
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PromptDto>(JsonOptions))!;
    }

    private static async Task<LinkedDocumentDto> LinkDocumentAsync(HttpClient client, Guid promptId, string planPath)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/prompts/{promptId}/linked-documents",
            new
            {
                absolutePath = planPath,
                documentType = LinkedDocumentType.ClaudeCodePlan,
                displayName = "Plano"
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<LinkedDocumentDto>(JsonOptions))!;
    }

    private static async Task<FutureTaskDto> CreateFutureTaskAsync(HttpClient client, Guid workingDirectoryId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/future-tasks",
            new
            {
                workingDirectoryId,
                title = "Backlog",
                description = "Executar depois",
                type = FutureTaskType.Task,
                labels = new[] { "backend" },
                issueGithubId = (string?)null
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<FutureTaskDto>(JsonOptions))!;
    }

    private static async Task<DiagramDto> CreateDiagramAsync(HttpClient client, Guid workingDirectoryId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/diagrams",
            new
            {
                workingDirectoryId,
                title = "Fluxo",
                type = DiagramType.Mermaid,
                content = "flowchart TD\nA --> B"
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<DiagramDto>(JsonOptions))!;
    }

    private static async Task<NotebookDto> CreateNotebookAsync(HttpClient client, Guid workingDirectoryId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/notebooks",
            new
            {
                title = "Notas",
                description = "Notas do workspace",
                workingDirectoryId
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<NotebookDto>(JsonOptions))!;
    }

    private static async Task<NoteDto> CreateNoteAsync(HttpClient client, Guid notebookId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/notes",
            new
            {
                notebookId,
                title = "Nota",
                contentMarkdown = "conteudo"
            },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<NoteDto>(JsonOptions))!;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
