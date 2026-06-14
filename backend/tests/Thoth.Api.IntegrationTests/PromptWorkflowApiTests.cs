using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;
using Thoth.Domain.Workflows;
using Thoth.Infrastructure.Persistence;

namespace Thoth.Api.IntegrationTests;

public sealed class PromptWorkflowApiTests(ThothApiFactory factory) : IClassFixture<ThothApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static StringContent JsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static string CreateWorkingDirectoryPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"repo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private async Task<WorkingDirectoryDto> CreateWorkingDirectoryAsync(HttpClient client, string? name = null)
    {
        var createDirectory = await client.PostAsync(
            "/api/working-directories",
            JsonContent(new { name = name ?? "repo", absolutePath = CreateWorkingDirectoryPath(), respectGitignore = true }));
        createDirectory.StatusCode.Should().Be(HttpStatusCode.Created, await createDirectory.Content.ReadAsStringAsync());
        var directory = await createDirectory.Content.ReadFromJsonAsync<WorkingDirectoryDto>(JsonOptions);
        return directory!;
    }

    private async Task<PromptDto> CreateRootPromptAsync(HttpClient client, string title)
    {
        var directory = await CreateWorkingDirectoryAsync(client);
        return await CreateRootPromptAsync(client, directory.Id, title);
    }

    private async Task<PromptDto> CreateRootPromptAsync(HttpClient client, Guid workingDirectoryId, string title)
    {
        var createPrompt = await client.PostAsync(
            "/api/prompts",
            JsonContent(new
            {
                workingDirectoryId,
                title,
                content = "conteúdo",
                targetAgent = "Codex",
                kind = "Planning",
                status = "Draft",
                mentions = Array.Empty<object>()
            }));
        createPrompt.StatusCode.Should().Be(HttpStatusCode.Created, await createPrompt.Content.ReadAsStringAsync());
        var prompt = await createPrompt.Content.ReadFromJsonAsync<PromptDto>(JsonOptions);
        return prompt!;
    }

    private async Task<WorkflowDto> PostWorkflowAsync(HttpClient client, Guid promptId, string action, object payload)
    {
        var response = await client.PostAsync($"/api/prompts/{promptId}/workflow/{action}", JsonContent(payload));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var dto = await response.Content.ReadFromJsonAsync<WorkflowDto>(JsonOptions);
        return dto!;
    }

    [Fact]
    public async Task Full_workflow_lifecycle_via_api()
    {
        var client = factory.CreateClient();
        var prompt = await CreateRootPromptAsync(client, "Tarefa do dia");

        var workflow = await client.GetFromJsonAsync<WorkflowDto>($"/api/prompts/{prompt.Id}/workflow", JsonOptions);
        workflow.Should().NotBeNull();
        workflow!.Status.Should().Be(PromptWorkflowStatus.Active);
        workflow.CurrentPhaseName.Should().Be("Engenharia de prompt");
        workflow.CurrentActor.Should().Be(WorkflowActor.Human);
        workflow.CurrentPhaseIteration.Should().Be(1);
        workflow.Phases.Should().Contain(phase => phase.Name == "Revisão do plano");

        workflow = await PostWorkflowAsync(client, prompt.Id, "advance", new { rowVersion = workflow.RowVersion, note = (string?)null });
        workflow.CurrentPhaseName.Should().Be("Planejamento");
        workflow.CurrentActor.Should().Be(WorkflowActor.ClaudeCode);
        workflow = await PostWorkflowAsync(client, prompt.Id, "advance", new { rowVersion = workflow.RowVersion, note = (string?)null });
        workflow.CurrentPhaseName.Should().Be("Revisão do plano");
        workflow = await PostWorkflowAsync(client, prompt.Id, "advance", new { rowVersion = workflow.RowVersion, note = (string?)null });
        workflow.CurrentPhaseName.Should().Be("Correção do plano");

        var reviewPhaseId = workflow.Phases.Single(phase => phase.Name == "Revisão do plano").Id;
        workflow = await PostWorkflowAsync(client, prompt.Id, "phase",
            new { phaseId = reviewPhaseId, actor = (string?)null, note = (string?)null, rowVersion = workflow.RowVersion });
        workflow.CurrentPhaseName.Should().Be("Revisão do plano");

        var conflict = await client.PostAsync(
            $"/api/prompts/{prompt.Id}/workflow/advance",
            JsonContent(new { rowVersion = "999999", note = (string?)null }));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        workflow = await PostWorkflowAsync(client, prompt.Id, "notes", new { note = "Codex pediu ajustes" });
        workflow.Events.Should().Contain(@event => @event.Type == WorkflowEventType.Note && @event.Note == "Codex pediu ajustes");

        workflow = await PostWorkflowAsync(client, prompt.Id, "complete", new { note = (string?)null, rowVersion = workflow.RowVersion });
        workflow.Status.Should().Be(PromptWorkflowStatus.Done);
        workflow = await PostWorkflowAsync(client, prompt.Id, "reopen", new { phaseId = (Guid?)null, rowVersion = workflow.RowVersion });
        workflow.Status.Should().Be(PromptWorkflowStatus.Active);

        var board = await client.GetFromJsonAsync<List<TaskSummaryDto>>("/api/workflow/board", JsonOptions);
        board.Should().Contain(summary => summary.PromptId == prompt.Id && summary.CurrentPhaseName == workflow.CurrentPhaseName);
        var boardSummary = board!.Single(summary => summary.PromptId == prompt.Id);
        boardSummary.Phases.Should().Contain(phase => phase.Name == "Revisão do plano");
        boardSummary.PromptRowVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Review_verdict_accepts_long_text_and_custom_review_correction_phase()
    {
        const string customCorrectionPhase = "Correcao de Pontos da Revisao";
        var client = factory.CreateClient();
        var prompt = await CreateRootPromptAsync(client, "Veredito longo");
        var workflow = await client.GetFromJsonAsync<WorkflowDto>($"/api/prompts/{prompt.Id}/workflow", JsonOptions);
        workflow.Should().NotBeNull();

        var phases = workflow!.Phases
            .Where(phase => phase.Name != "Corre\u00e7\u00e3o da revis\u00e3o")
            .Select((phase, index) => (object)new
            {
                id = (Guid?)phase.Id,
                name = phase.Name,
                defaultActor = phase.DefaultActor.ToString(),
                orderIndex = index,
                color = phase.Color
            })
            .ToList();
        phases.Add(new
        {
            id = (Guid?)null,
            name = customCorrectionPhase,
            defaultActor = WorkflowActor.Codex.ToString(),
            orderIndex = phases.Count,
            color = "#dc2626"
        });

        var updatePhases = await client.PutAsync(
            $"/api/prompts/{prompt.Id}/workflow/phases",
            JsonContent(new { phases, rowVersion = workflow.RowVersion }));
        updatePhases.StatusCode.Should().Be(HttpStatusCode.OK, await updatePhases.Content.ReadAsStringAsync());
        workflow = await updatePhases.Content.ReadFromJsonAsync<WorkflowDto>(JsonOptions);
        workflow!.Phases.Single(phase => phase.Name == customCorrectionPhase).Role.Should().Be(WorkflowPhaseRole.ReviewCorrection);

        while (workflow.CurrentPhaseName != "Revis\u00e3o de c\u00f3digo")
        {
            workflow = await PostWorkflowAsync(client, prompt.Id, "advance", new { rowVersion = workflow.RowVersion, note = (string?)null });
        }

        var verdict = new string('x', 5504);
        var response = await client.PostAsync(
            $"/api/prompts/{prompt.Id}/workflow/review-verdict",
            JsonContent(new { verdict, rowVersion = workflow.RowVersion }));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var corrected = await response.Content.ReadFromJsonAsync<WorkflowDto>(JsonOptions);
        corrected!.CurrentPhaseName.Should().Be(customCorrectionPhase);
        corrected.Events.Should().Contain(@event => @event.Type == WorkflowEventType.Note && @event.Note == verdict);
    }

    [Fact]
    public async Task Board_hides_archived_prompts_by_default()
    {
        var client = factory.CreateClient();
        var prompt = await CreateRootPromptAsync(client, "Tarefa arquivável");

        var rowVersion = (await client.GetFromJsonAsync<PromptDto>($"/api/prompts/{prompt.Id}", JsonOptions))!.RowVersion;
        var archive = await client.PatchAsync(
            $"/api/prompts/{prompt.Id}/status",
            JsonContent(new { status = "Archived", rowVersion }));
        archive.StatusCode.Should().Be(HttpStatusCode.OK, await archive.Content.ReadAsStringAsync());

        var defaultBoard = await client.GetFromJsonAsync<List<TaskSummaryDto>>("/api/workflow/board", JsonOptions);
        defaultBoard.Should().NotContain(summary => summary.PromptId == prompt.Id);

        var archivedBoard = await client.GetFromJsonAsync<List<TaskSummaryDto>>("/api/workflow/board?promptStatus=Archived", JsonOptions);
        archivedBoard.Should().Contain(summary => summary.PromptId == prompt.Id);
    }

    [Fact]
    public async Task Editing_template_applies_to_new_tasks()
    {
        var client = factory.CreateClient();
        var template = await client.GetFromJsonAsync<WorkflowTemplateDto>("/api/workflow/template", JsonOptions);
        template.Should().NotBeNull();

        var originalPhases = template!.Phases
            .Select(phase => (object)new
            {
                id = (Guid?)phase.Id,
                name = phase.Name,
                defaultActor = phase.DefaultActor.ToString(),
                orderIndex = phase.OrderIndex,
                color = phase.Color
            })
            .ToList();

        var withDeploy = originalPhases
            .Append(new { id = (Guid?)null, name = "Deploy", defaultActor = "Human", orderIndex = template.Phases.Count, color = "#15803d" })
            .ToList();

        try
        {
            var put = await client.PutAsync("/api/workflow/template", JsonContent(new { phases = withDeploy }));
            put.StatusCode.Should().Be(HttpStatusCode.OK, await put.Content.ReadAsStringAsync());
            var updated = await put.Content.ReadFromJsonAsync<WorkflowTemplateDto>(JsonOptions);
            updated!.Phases.Should().Contain(phase => phase.Name == "Deploy");

            var prompt = await CreateRootPromptAsync(client, "Tarefa pós-template");
            var workflow = await client.GetFromJsonAsync<WorkflowDto>($"/api/prompts/{prompt.Id}/workflow", JsonOptions);
            workflow!.Phases.Should().Contain(phase => phase.Name == "Deploy");
        }
        finally
        {
            await client.PutAsync("/api/workflow/template", JsonContent(new { phases = originalPhases }));
        }
    }

    [Fact]
    public async Task Workflow_changes_are_broadcast_over_signalr()
    {
        var client = factory.CreateClient();

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, "/hubs/prompts"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        var received = new TaskCompletionSource<TaskSummaryDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TaskSummaryDto>("TaskWorkflowChanged", summary =>
        {
            if (summary.Title == "Tarefa em tempo real")
            {
                received.TrySetResult(summary);
            }
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinTasks");

        try
        {
            var prompt = await CreateRootPromptAsync(client, "Tarefa em tempo real");
            var workflow = await client.GetFromJsonAsync<WorkflowDto>($"/api/prompts/{prompt.Id}/workflow", JsonOptions);
            await client.PostAsync(
                $"/api/prompts/{prompt.Id}/workflow/advance",
                JsonContent(new { rowVersion = workflow!.RowVersion, note = (string?)null }));

            var winner = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            winner.Should().Be(received.Task, "the workflow change should reach the tasks:all SignalR group");
            (await received.Task).PromptId.Should().Be(prompt.Id);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Reorder_board_column_persists_ranks_without_touching_updated_at_and_broadcasts()
    {
        var client = factory.CreateClient();
        var directory = await CreateWorkingDirectoryAsync(client, "repo-reorder");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 13, 0, 0, TimeSpan.Zero));
        var first = await CreateRootPromptAsync(client, directory.Id, "Primeiro");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 13, 5, 0, TimeSpan.Zero));
        var second = await CreateRootPromptAsync(client, directory.Id, "Segundo");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 13, 10, 0, TimeSpan.Zero));
        var third = await CreateRootPromptAsync(client, directory.Id, "Terceiro");
        var promptIds = new[] { first.Id, second.Id, third.Id };

        Dictionary<Guid, DateTimeOffset> before;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            before = await db.Prompts
                .AsNoTracking()
                .Where(prompt => promptIds.Contains(prompt.Id))
                .ToDictionaryAsync(prompt => prompt.Id, prompt => prompt.UpdatedAtUtc);
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, "/hubs/prompts"),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On("BoardReordered", () => received.TrySetResult(true));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinTasks");

        try
        {
            var response = await client.PostAsync(
                "/api/prompts/board/reorder",
                JsonContent(new { orderedPromptIds = new[] { third.Id, first.Id, second.Id } }));

            response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
            var winner = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            winner.Should().Be(received.Task, "board reorder should reach the tasks:all SignalR group");
        }
        finally
        {
            await connection.DisposeAsync();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var after = await db.Prompts
                .AsNoTracking()
                .Where(prompt => promptIds.Contains(prompt.Id))
                .ToDictionaryAsync(prompt => prompt.Id);

            after[third.Id].BoardRank.Should().Be(1);
            after[first.Id].BoardRank.Should().Be(2);
            after[second.Id].BoardRank.Should().Be(3);
            after.Should().OnlyContain(item => item.Value.UpdatedAtUtc == before[item.Key]);
        }

        var board = await client.GetFromJsonAsync<List<TaskSummaryDto>>(
            $"/api/workflow/board?workingDirectoryId={directory.Id}",
            JsonOptions);
        board!.Select(summary => summary.PromptId).Should().Equal(third.Id, first.Id, second.Id);
    }

    [Fact]
    public async Task Reorder_board_column_rejects_prompt_from_another_owner()
    {
        var client = factory.CreateClient();
        var mine = await CreateRootPromptAsync(client, "Meu prompt");
        var otherPromptId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        var otherDirectoryId = Guid.CreateVersion7();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new User
            {
                Id = otherOwnerId,
                DisplayName = "other",
                IsSystem = false
            });
            db.WorkingDirectories.Add(new WorkingDirectory
            {
                Id = otherDirectoryId,
                Name = "other-repo",
                AbsolutePath = CreateWorkingDirectoryPath(),
                RespectGitignore = true,
                OwnerId = otherOwnerId
            });
            db.Prompts.Add(new Prompt
            {
                Id = otherPromptId,
                WorkingDirectoryId = otherDirectoryId,
                Title = "Prompt de outro usuario",
                Content = "conteudo",
                OwnerId = otherOwnerId,
                Status = PromptStatus.Draft
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsync(
            "/api/prompts/board/reorder",
            JsonContent(new { orderedPromptIds = new[] { mine.Id, otherPromptId } }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Board_orders_by_board_rank_then_updated_at_desc()
    {
        var client = factory.CreateClient();
        var directory = await CreateWorkingDirectoryAsync(client, "repo-board-order");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero));
        var olderUnranked = await CreateRootPromptAsync(client, directory.Id, "Mais antigo sem rank");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 14, 5, 0, TimeSpan.Zero));
        var newerUnranked = await CreateRootPromptAsync(client, directory.Id, "Mais novo sem rank");
        factory.Clock.Set(new DateTimeOffset(2026, 6, 1, 14, 10, 0, TimeSpan.Zero));
        var ranked = await CreateRootPromptAsync(client, directory.Id, "Com rank");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Prompts
                .Where(prompt => prompt.Id == ranked.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(prompt => prompt.BoardRank, 2d));
        }

        var board = await client.GetFromJsonAsync<List<TaskSummaryDto>>(
            $"/api/workflow/board?workingDirectoryId={directory.Id}",
            JsonOptions);

        board!.Select(summary => summary.PromptId).Should().Equal(newerUnranked.Id, olderUnranked.Id, ranked.Id);
    }
}
