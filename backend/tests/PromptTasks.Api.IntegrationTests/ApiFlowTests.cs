using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;
using PromptTasks.Infrastructure.Persistence;

namespace PromptTasks.Api.IntegrationTests;

public sealed class ApiFlowTests(PromptTasksApiFactory factory) : IClassFixture<PromptTasksApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"prompttasks-api-{Guid.NewGuid():N}");

    [Fact]
    public async Task Product_flow_persists_versions_references_concurrency_and_signalr_events()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "src"));
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "src", "main.go"), "package main");

        var client = factory.CreateClient();

        var invalidResponse = await client.PostAsJsonAsync(
            "/api/working-directories/validate-path",
            new { absolutePath = Path.Combine(_tempRoot, "missing") },
            JsonOptions);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invalidPath = await invalidResponse.Content.ReadFromJsonAsync<ValidatePathResponse>(JsonOptions);
        invalidPath!.IsValid.Should().BeFalse();

        var wdResponse = await client.PostAsJsonAsync(
            "/api/working-directories",
            new { name = "repo", absolutePath = _tempRoot, respectGitignore = true },
            JsonOptions);
        wdResponse.StatusCode.Should().Be(HttpStatusCode.Created, await wdResponse.Content.ReadAsStringAsync());
        var wd = await wdResponse.Content.ReadFromJsonAsync<WorkingDirectoryDto>(JsonOptions);
        wd.Should().NotBeNull();

        var search = await client.GetFromJsonAsync<FileSearchResultDto[]>(
            $"/api/files/search?workingDirectoryId={wd!.Id}&query=main&limit=20",
            JsonOptions);
        search.Should().Contain(item => item.RelativePath == "src/main.go");

        await using var hub = CreateHubConnection();
        var createdTcs = new TaskCompletionSource<PromptDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updatedTcs = new TaskCompletionSource<PromptDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deletedTcs = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<PromptDto>("PromptCreated", prompt => createdTcs.TrySetResult(prompt));
        hub.On<PromptDto>("PromptUpdated", prompt => updatedTcs.TrySetResult(prompt));
        hub.On<Guid, Guid>("PromptDeleted", (promptId, _) => deletedTcs.TrySetResult(promptId));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinWorkingDirectory", wd.Id);

        var createResponse = await client.PostAsJsonAsync(
            "/api/prompts",
            new
            {
                workingDirectoryId = wd.Id,
                title = "Inspect main",
                content = "Please inspect @src/main.go",
                targetAgent = TargetAgent.Codex,
                kind = PromptKind.General,
                status = PromptStatus.Draft,
                mentions = new[] { new { id = "src/main.go", label = "src/main.go" } }
            },
            JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PromptDto>(JsonOptions);
        created.Should().NotBeNull();
        (await createdTcs.Task.WaitAsync(TimeSpan.FromSeconds(10))).Id.Should().Be(created!.Id);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Prompts.Should().ContainSingle();
            db.PromptVersions.Should().ContainSingle(version => version.VersionNumber == 1);
            db.PromptFileReferences.Should().ContainSingle(reference => reference.RelativePath == "src/main.go");
        }

        var current = await client.GetFromJsonAsync<PromptDto>($"/api/prompts/{created.Id}", JsonOptions);
        current.Should().NotBeNull();

        var updatePayload = new
        {
            title = "Inspect main updated",
            content = "Please inspect @src/main.go again",
            targetAgent = TargetAgent.Codex,
            kind = PromptKind.General,
            status = PromptStatus.Ready,
            rowVersion = current!.RowVersion,
            mentions = new[] { new { id = "src/main.go", label = "src/main.go" } }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/prompts/{created.Id}", updatePayload, JsonOptions);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PromptDto>(JsonOptions);
        updated!.CurrentVersion.Should().Be(2);
        (await updatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10))).Id.Should().Be(created.Id);

        var conflictResponse = await client.PutAsJsonAsync($"/api/prompts/{created.Id}", updatePayload, JsonOptions);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var versions = await client.GetFromJsonAsync<PromptVersionDto[]>($"/api/prompts/{created.Id}/versions", JsonOptions);
        versions.Should().HaveCount(2);

        var deleteResponse = await client.DeleteAsync($"/api/prompts/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await deletedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10))).Should().Be(created.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private HubConnection CreateHubConnection()
    {
        factory.CreateClient();

        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/prompts"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();
    }
}
