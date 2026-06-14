using FluentAssertions;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;
using Thoth.Application.Features.Terminals.Commands.CreateGenericTerminalSession;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;

namespace Thoth.Application.UnitTests;

public sealed class CreateGenericTerminalSessionHandlerTests
{
    [Fact]
    public async Task Handle_creates_generic_session_for_current_user()
    {
        var context = new FakeApplicationDbContext();
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(context, new FakeCurrentUser(), coordinator);

        var result = await handler.Handle(
            new CreateGenericTerminalSessionCommand("pwsh.exe", null),
            CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.OwnerId.Should().Be(User.SystemUserId);
        coordinator.LastCreate.Value.Cwd.Should().BeNull();
        coordinator.LastCreate.Value.Shell.Should().Be("pwsh.exe");
        coordinator.LastCreate.Value.InitialInput.Should().BeNull();
        result.PromptId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_forwards_agent_launch_as_initial_input()
    {
        var context = new FakeApplicationDbContext();
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(context, new FakeCurrentUser(), coordinator);

        await handler.Handle(
            new CreateGenericTerminalSessionCommand(null, TerminalAgentLaunch.Codex),
            CancellationToken.None);

        coordinator.LastCreate!.Value.InitialInput.Should().BeEquivalentTo("codex --yolo\r"u8.ToArray());
    }

    [Fact]
    public async Task Handle_rejects_claude_plan_launch()
    {
        var context = new FakeApplicationDbContext();
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(context, new FakeCurrentUser(), coordinator);

        var act = () => handler.Handle(
            new CreateGenericTerminalSessionCommand(null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        coordinator.LastCreate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_uses_selected_workspace_as_initial_directory()
    {
        var context = new FakeApplicationDbContext();
        var workspace = SeedWorkingDirectory(context, "D:/repo", User.SystemUserId);
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(context, new FakeCurrentUser(), coordinator);

        await handler.Handle(
            new CreateGenericTerminalSessionCommand(null, null, workspace.Id),
            CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.Cwd.Should().Be(workspace.AbsolutePath);
    }

    [Fact]
    public async Task Handle_rejects_workspace_from_another_user()
    {
        var context = new FakeApplicationDbContext();
        var workspace = SeedWorkingDirectory(context, "D:/other", Guid.CreateVersion7());
        var coordinator = new RecordingTerminalCoordinator();
        var handler = new CreateGenericTerminalSessionHandler(context, new FakeCurrentUser(), coordinator);

        var act = () => handler.Handle(
            new CreateGenericTerminalSessionCommand(null, null, workspace.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        coordinator.LastCreate.Should().BeNull();
    }

    private static WorkingDirectory SeedWorkingDirectory(
        FakeApplicationDbContext context,
        string absolutePath,
        Guid ownerId)
    {
        var directory = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = Path.GetFileName(absolutePath.TrimEnd('/')),
            AbsolutePath = absolutePath,
            OwnerId = ownerId
        };
        context.WorkingDirectoryItems.Add(directory);
        return directory;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }

    private sealed class FakeApplicationDbContext : IApplicationDbContext
    {
        public List<WorkingDirectory> WorkingDirectoryItems { get; } = new();

        public IQueryable<User> Users => Enumerable.Empty<User>().AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => WorkingDirectoryItems.AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTask> FutureTasks => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTask>().AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTaskLabel> FutureTaskLabels => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTaskLabel>().AsQueryable();
        public IQueryable<Prompt> Prompts => Enumerable.Empty<Prompt>().AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => Enumerable.Empty<PromptVersion>().AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => Enumerable.Empty<PromptFileReference>().AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => Enumerable.Empty<LinkedDocument>().AsQueryable();
        public IQueryable<LinkedDocumentVersion> LinkedDocumentVersions => Enumerable.Empty<LinkedDocumentVersion>().AsQueryable();
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

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class RecordingTerminalCoordinator : ITerminalSessionCoordinator
    {
        public (Guid OwnerId, string? Cwd, string Shell, byte[]? InitialInput, byte[]? FollowUpInput)? LastCreate
        {
            get;
            private set;
        }

        public Task<TerminalSessionDescriptor> CreateGenericAsync(
            Guid ownerId,
            string? cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null)
        {
            LastCreate = (ownerId, cwd, shell, initialInput, followUpInput);
            return Task.FromResult(new TerminalSessionDescriptor(
                Guid.CreateVersion7(),
                null,
                shell,
                cwd ?? "C:/Users/dev",
                DateTimeOffset.UtcNow));
        }

        public Task<TerminalSessionDescriptor> CreateAsync(
            Guid promptId,
            string cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null) =>
            throw new NotSupportedException();

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
            Array.Empty<TerminalSessionDescriptor>();

        public IReadOnlyList<TerminalSessionDescriptor> ListForOwner(Guid ownerId) =>
            Array.Empty<TerminalSessionDescriptor>();

        public IReadOnlyList<TerminalSessionDescriptor> ListAll() =>
            Array.Empty<TerminalSessionDescriptor>();

        public TerminalSessionDescriptor? TryGetSession(Guid sessionId) => null;

        public TerminalOutputHistoryDto? GetOutputHistory(Guid sessionId) => null;

        public Task KillForPromptAsync(Guid promptId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
