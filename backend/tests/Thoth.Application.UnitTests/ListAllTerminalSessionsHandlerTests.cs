using FluentAssertions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals.Queries.ListAllTerminalSessions;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;

namespace Thoth.Application.UnitTests;

public sealed class ListAllTerminalSessionsHandlerTests
{
    private static readonly Guid OtherOwnerId = Guid.CreateVersion7();

    [Fact]
    public async Task Handle_returns_only_prompts_owned_by_current_user()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "repo");
        var ownedPrompt = SeedPrompt(context, directory.Id, User.SystemUserId);
        var foreignPrompt = SeedPrompt(context, directory.Id, OtherOwnerId);

        var coordinator = new StubTerminalCoordinator(
            Descriptor(ownedPrompt.Id, At(10)),
            Descriptor(foreignPrompt.Id, At(11)));
        var handler = new ListAllTerminalSessionsHandler(context, new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(new ListAllTerminalSessionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].PromptId.Should().Be(ownedPrompt.Id);
    }

    [Fact]
    public async Task Handle_groups_sessions_by_prompt_ordered_by_creation()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "repo");
        var promptA = SeedPrompt(context, directory.Id, User.SystemUserId);
        var promptB = SeedPrompt(context, directory.Id, User.SystemUserId);

        var olderA = Descriptor(promptA.Id, At(9));
        var newerA = Descriptor(promptA.Id, At(12));
        var onlyB = Descriptor(promptB.Id, At(10));
        var coordinator = new StubTerminalCoordinator(newerA, onlyB, olderA);
        var handler = new ListAllTerminalSessionsHandler(context, new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(new ListAllTerminalSessionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(group => group.PromptId == promptA.Id).Terminals
            .Select(terminal => terminal.Id).Should().ContainInOrder(olderA.Id, newerA.Id);
        result.Single(group => group.PromptId == promptB.Id).Terminals.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_projects_prompt_and_workspace_metadata()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "my-repo");
        var prompt = SeedPrompt(
            context,
            directory.Id,
            User.SystemUserId,
            status: PromptStatus.Archived,
            title: "Refatorar auth");

        var coordinator = new StubTerminalCoordinator(Descriptor(prompt.Id, At(10)));
        var handler = new ListAllTerminalSessionsHandler(context, new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(new ListAllTerminalSessionsQuery(), CancellationToken.None);

        var group = result.Should().ContainSingle().Subject;
        group.PromptTitle.Should().Be("Refatorar auth");
        group.WorkingDirectoryId.Should().Be(directory.Id);
        group.WorkingDirectoryName.Should().Be("my-repo");
        group.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_returns_empty_when_no_sessions()
    {
        var context = new FakeApplicationDbContext();
        var handler = new ListAllTerminalSessionsHandler(context, new FakeCurrentUser(), new StubTerminalCoordinator());

        var result = await handler.Handle(new ListAllTerminalSessionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_drops_sessions_whose_prompt_is_missing()
    {
        var context = new FakeApplicationDbContext();
        var coordinator = new StubTerminalCoordinator(Descriptor(Guid.CreateVersion7(), At(10)));
        var handler = new ListAllTerminalSessionsHandler(context, new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(new ListAllTerminalSessionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static DateTimeOffset At(int hour) => new(2026, 6, 13, hour, 0, 0, TimeSpan.Zero);

    private static TerminalSessionDescriptor Descriptor(Guid promptId, DateTimeOffset createdAt) =>
        new(Guid.CreateVersion7(), promptId, "pwsh.exe", "D:/repo", createdAt);

    private static WorkingDirectory SeedWorkingDirectory(FakeApplicationDbContext context, string name)
    {
        var directory = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            AbsolutePath = $"D:/{name}",
            OwnerId = User.SystemUserId
        };
        context.WorkingDirectoryItems.Add(directory);
        return directory;
    }

    private static Prompt SeedPrompt(
        FakeApplicationDbContext context,
        Guid workingDirectoryId,
        Guid ownerId,
        PromptStatus status = PromptStatus.Ready,
        string title = "Prompt")
    {
        var prompt = new Prompt
        {
            Id = Guid.CreateVersion7(),
            WorkingDirectoryId = workingDirectoryId,
            Title = title,
            Content = "Content",
            TargetAgent = TargetAgent.ClaudeCode,
            Kind = PromptKind.General,
            Status = status,
            CurrentVersion = 1,
            OwnerId = ownerId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        context.PromptItems.Add(prompt);
        return prompt;
    }

    private sealed class StubTerminalCoordinator(params TerminalSessionDescriptor[] sessions)
        : ITerminalSessionCoordinator
    {
        public Task<TerminalSessionDescriptor> CreateAsync(
            Guid promptId,
            string cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null) =>
            throw new NotSupportedException();

        public Task<TerminalSessionDescriptor> CreateGenericAsync(
            Guid ownerId,
            string? cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null) =>
            throw new NotSupportedException();

        public IReadOnlyList<TerminalSessionDescriptor> ListForOwner(Guid ownerId) =>
            sessions.Where(session => session.PromptId is null).ToList();

        public void WriteInput(Guid sessionId, byte[] input)
        {
        }

        public void Resize(Guid sessionId, ushort cols, ushort rows)
        {
        }

        public Task CloseAsync(Guid sessionId, CancellationToken cancellationToken) => Task.CompletedTask;

        public void AttachConnection(Guid sessionId, string connectionId)
        {
        }

        public void DetachConnection(Guid sessionId, string connectionId)
        {
        }

        public void ReleaseConnection(string connectionId)
        {
        }

        public IReadOnlyList<TerminalSessionDescriptor> ListForPrompt(Guid promptId) =>
            sessions.Where(session => session.PromptId == promptId).ToList();

        public IReadOnlyList<TerminalSessionDescriptor> ListAll() => sessions;

        public TerminalSessionDescriptor? TryGetSession(Guid sessionId) =>
            sessions.FirstOrDefault(session => session.Id == sessionId);

        public TerminalOutputHistoryDto? GetOutputHistory(Guid sessionId) => null;

        public Task KillForPromptAsync(Guid promptId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }

    private sealed class FakeApplicationDbContext : IApplicationDbContext
    {
        public List<User> UserItems { get; } = new();
        public List<WorkingDirectory> WorkingDirectoryItems { get; } = new();
        public List<Prompt> PromptItems { get; } = new();
        public List<PromptVersion> PromptVersionItems { get; } = new();
        public List<PromptFileReference> PromptFileReferenceItems { get; } = new();
        public List<LinkedDocument> LinkedDocumentItems { get; } = new();
        public List<LinkedDocumentVersion> LinkedDocumentVersionItems { get; } = new();

        public IQueryable<User> Users => UserItems.AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => WorkingDirectoryItems.AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTask> FutureTasks => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTask>().AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTaskLabel> FutureTaskLabels => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTaskLabel>().AsQueryable();
        public IQueryable<Prompt> Prompts => PromptItems.AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => PromptVersionItems.AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => PromptFileReferenceItems.AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => LinkedDocumentItems.AsQueryable();
        public IQueryable<LinkedDocumentVersion> LinkedDocumentVersions => LinkedDocumentVersionItems.AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.WorkflowTemplate> WorkflowTemplates => Enumerable.Empty<Thoth.Domain.Workflows.WorkflowTemplate>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.WorkflowTemplatePhase> WorkflowTemplatePhases => Enumerable.Empty<Thoth.Domain.Workflows.WorkflowTemplatePhase>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflow> PromptWorkflows => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflow>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflowPhase> PromptWorkflowPhases => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflowPhase>().AsQueryable();
        public IQueryable<Thoth.Domain.Workflows.PromptWorkflowEvent> PromptWorkflowEvents => Enumerable.Empty<Thoth.Domain.Workflows.PromptWorkflowEvent>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatSession> AiChatSessions => Enumerable.Empty<Thoth.Domain.Ai.AiChatSession>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatMessage> AiChatMessages => Enumerable.Empty<Thoth.Domain.Ai.AiChatMessage>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiUserSettings> AiUserSettings => Enumerable.Empty<Thoth.Domain.Ai.AiUserSettings>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Notebook> Notebooks => Enumerable.Empty<Thoth.Domain.Notebooks.Notebook>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Note> Notes => Enumerable.Empty<Thoth.Domain.Notebooks.Note>().AsQueryable();
        public IQueryable<Thoth.Domain.Diagrams.Diagram> Diagrams => Enumerable.Empty<Thoth.Domain.Diagrams.Diagram>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
        }

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
        }

        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }
}
