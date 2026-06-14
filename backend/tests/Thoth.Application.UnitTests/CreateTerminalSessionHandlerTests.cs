using FluentAssertions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Terminals;
using Thoth.Application.Features.Terminals.Commands.CreateTerminalSession;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;
using Thoth.Domain.Workflows;

namespace Thoth.Application.UnitTests;

public sealed class CreateTerminalSessionHandlerTests
{
    [Fact]
    public async Task Handle_uses_parent_prompt_workspace_for_child_prompts()
    {
        var context = new FakeApplicationDbContext();
        var parentDirectory = SeedWorkingDirectory(context, "D:/parent-repo");
        var childDirectory = SeedWorkingDirectory(context, "D:/child-repo");
        var parentPrompt = SeedPrompt(context, parentDirectory.Id, parentPromptId: null);
        var childPrompt = SeedPrompt(context, childDirectory.Id, parentPrompt.Id);

        var coordinator = new RecordingTerminalCoordinator();
        var handler = CreateHandler(context, coordinator);

        await handler.Handle(new CreateTerminalSessionCommand(childPrompt.Id, null, null), CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.PromptId.Should().Be(childPrompt.Id);
        coordinator.LastCreate.Value.Cwd.Should().Be(parentDirectory.AbsolutePath);
    }

    [Fact]
    public async Task Handle_passes_agent_launch_command_as_initial_input()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);

        var coordinator = new RecordingTerminalCoordinator();
        var handler = CreateHandler(context, coordinator);

        await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.Claude),
            CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.InitialInput.Should().BeEquivalentTo(
            "claude --dangerously-skip-permissions --effort max\r"u8.ToArray());
    }

    [Fact]
    public async Task Handle_passes_prompt_content_for_claude_plan_launch()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null, content: "Planeje @arquivo.md com café");

        var coordinator = new RecordingTerminalCoordinator();
        var handler = CreateHandler(context, coordinator);

        await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        var launch = System.Text.Encoding.UTF8.GetString(coordinator.LastCreate!.Value.InitialInput!);
        launch.Should().Contain("claude --effort max --permission-mode plan --settings $p\r");
        launch.Should().NotContain("--dangerously-skip-permissions");

        var followUp = System.Text.Encoding.UTF8.GetString(coordinator.LastCreate.Value.FollowUpInput!);
        followUp.Should().NotStartWith("/plan");
        followUp.Should().Contain("Planeje @arquivo.md com café");
    }

    [Fact]
    public async Task Handle_uses_prompt_workspace_for_root_prompts()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);

        var coordinator = new RecordingTerminalCoordinator();
        var handler = CreateHandler(context, coordinator);

        await handler.Handle(new CreateTerminalSessionCommand(prompt.Id, "powershell.exe", null), CancellationToken.None);

        coordinator.LastCreate.Should().NotBeNull();
        coordinator.LastCreate!.Value.PromptId.Should().Be(prompt.Id);
        coordinator.LastCreate.Value.Cwd.Should().Be(directory.AbsolutePath);
        coordinator.LastCreate.Value.Shell.Should().Be("powershell.exe");
    }

    [Fact]
    public async Task ClaudePlan_for_root_prompt_enters_planning_and_notifies_board()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);
        var workflow = SeedWorkflow(context, prompt, WorkflowPhaseRole.PromptEngineering);
        var coordinator = new RecordingTerminalCoordinator();
        var notifier = new RecordingWorkflowNotifier();
        var handler = CreateHandler(context, coordinator, notifier);

        await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        workflow.CurrentPhaseName.Should().Be("Planejamento");
        workflow.CurrentActor.Should().Be(WorkflowActor.ClaudeCode);
        context.PromptWorkflowEventItems.Should().ContainSingle(@event =>
            @event.Type == WorkflowEventType.PhaseChanged &&
            @event.Note == "Plan mode iniciado" &&
            @event.PhaseNameSnapshot == "Planejamento");
        notifier.Changes.Should().ContainSingle(summary =>
            summary.PromptId == prompt.Id &&
            summary.CurrentPhaseName == "Planejamento" &&
            summary.CurrentActor == WorkflowActor.ClaudeCode);
    }

    [Fact]
    public async Task Non_plan_launch_does_not_change_workflow()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);
        var workflow = SeedWorkflow(context, prompt, WorkflowPhaseRole.PromptEngineering);
        var coordinator = new RecordingTerminalCoordinator();
        var notifier = new RecordingWorkflowNotifier();
        var handler = CreateHandler(context, coordinator, notifier);

        await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.Claude),
            CancellationToken.None);

        workflow.CurrentPhaseName.Should().Be("Engenharia de prompt");
        context.PromptWorkflowEventItems.Should().BeEmpty();
        notifier.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ClaudePlan_does_not_regress_workflows_already_past_planning()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);
        var workflow = SeedWorkflow(context, prompt, WorkflowPhaseRole.PlanReview);
        var coordinator = new RecordingTerminalCoordinator();
        var notifier = new RecordingWorkflowNotifier();
        var handler = CreateHandler(context, coordinator, notifier);

        await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        workflow.CurrentPhaseName.Should().Be(
            WorkflowDefaults.Phases.Single(phase => phase.Role == WorkflowPhaseRole.PlanReview).Name);
        context.PromptWorkflowEventItems.Should().BeEmpty();
        notifier.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ClaudePlan_for_child_prompt_does_not_change_parent_workflow()
    {
        var context = new FakeApplicationDbContext();
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var parent = SeedPrompt(context, directory.Id, parentPromptId: null);
        var child = SeedPrompt(context, directory.Id, parent.Id);
        var workflow = SeedWorkflow(context, parent, WorkflowPhaseRole.PromptEngineering);
        var coordinator = new RecordingTerminalCoordinator();
        var notifier = new RecordingWorkflowNotifier();
        var handler = CreateHandler(context, coordinator, notifier);

        await handler.Handle(
            new CreateTerminalSessionCommand(child.Id, null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        workflow.CurrentPhaseName.Should().Be("Engenharia de prompt");
        context.PromptWorkflowEventItems.Should().BeEmpty();
        notifier.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Workflow_update_failure_does_not_block_terminal_creation()
    {
        var context = new FakeApplicationDbContext { ThrowOnSave = true };
        var directory = SeedWorkingDirectory(context, "D:/repo");
        var prompt = SeedPrompt(context, directory.Id, parentPromptId: null);
        SeedWorkflow(context, prompt, WorkflowPhaseRole.PromptEngineering);
        var coordinator = new RecordingTerminalCoordinator();
        var handler = CreateHandler(context, coordinator);

        var descriptor = await handler.Handle(
            new CreateTerminalSessionCommand(prompt.Id, null, TerminalAgentLaunch.ClaudePlan),
            CancellationToken.None);

        descriptor.PromptId.Should().Be(prompt.Id);
        coordinator.LastCreate.Should().NotBeNull();
    }

    private static CreateTerminalSessionHandler CreateHandler(
        FakeApplicationDbContext context,
        RecordingTerminalCoordinator coordinator,
        RecordingWorkflowNotifier? notifier = null,
        FakeClock? clock = null) =>
        new(
            context,
            new FakeCurrentUser(),
            coordinator,
            notifier ?? new RecordingWorkflowNotifier(),
            clock ?? new FakeClock());

    private static WorkingDirectory SeedWorkingDirectory(FakeApplicationDbContext context, string absolutePath)
    {
        var directory = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = Path.GetFileName(absolutePath.TrimEnd('/')),
            AbsolutePath = absolutePath,
            OwnerId = User.SystemUserId
        };
        context.WorkingDirectoryItems.Add(directory);
        return directory;
    }

    private static Prompt SeedPrompt(
        FakeApplicationDbContext context,
        Guid workingDirectoryId,
        Guid? parentPromptId,
        string content = "Content")
    {
        var prompt = new Prompt
        {
            Id = Guid.CreateVersion7(),
            WorkingDirectoryId = workingDirectoryId,
            ParentPromptId = parentPromptId,
            Title = "Prompt",
            Content = content,
            TargetAgent = TargetAgent.ClaudeCode,
            Kind = PromptKind.General,
            Status = PromptStatus.Ready,
            CurrentVersion = 1,
            OwnerId = User.SystemUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        context.PromptItems.Add(prompt);
        return prompt;
    }

    private static PromptWorkflow SeedWorkflow(
        FakeApplicationDbContext context,
        Prompt prompt,
        WorkflowPhaseRole currentRole)
    {
        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var workflow = new PromptWorkflow
        {
            Id = Guid.CreateVersion7(),
            PromptId = prompt.Id,
            Status = PromptWorkflowStatus.Active,
            StartedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var phases = WorkflowDefaults.Phases
            .Select((seed, index) => new PromptWorkflowPhase
            {
                Id = Guid.CreateVersion7(),
                PromptWorkflowId = workflow.Id,
                Name = seed.Name,
                DefaultActor = seed.DefaultActor,
                OrderIndex = index,
                Color = seed.Color,
                Role = seed.Role
            })
            .ToList();
        var current = phases.Single(phase => phase.Role == currentRole);
        workflow.CurrentPhaseId = current.Id;
        workflow.CurrentPhaseName = current.Name;
        workflow.CurrentPhaseColor = current.Color;
        workflow.CurrentActor = current.DefaultActor;
        workflow.CurrentPhaseIteration = 1;
        workflow.EnteredCurrentPhaseAtUtc = now;

        context.PromptWorkflowItems.Add(workflow);
        context.PromptWorkflowPhaseItems.AddRange(phases);
        return workflow;
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
        public List<WorkflowTemplate> WorkflowTemplateItems { get; } = new();
        public List<WorkflowTemplatePhase> WorkflowTemplatePhaseItems { get; } = new();
        public List<PromptWorkflow> PromptWorkflowItems { get; } = new();
        public List<PromptWorkflowPhase> PromptWorkflowPhaseItems { get; } = new();
        public List<PromptWorkflowEvent> PromptWorkflowEventItems { get; } = new();
        public bool ThrowOnSave { get; init; }

        public IQueryable<User> Users => UserItems.AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => WorkingDirectoryItems.AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTask> FutureTasks => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTask>().AsQueryable();
        public IQueryable<Thoth.Domain.FutureTasks.FutureTaskLabel> FutureTaskLabels => Enumerable.Empty<Thoth.Domain.FutureTasks.FutureTaskLabel>().AsQueryable();
        public IQueryable<Prompt> Prompts => PromptItems.AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => PromptVersionItems.AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => PromptFileReferenceItems.AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => LinkedDocumentItems.AsQueryable();
        public IQueryable<LinkedDocumentVersion> LinkedDocumentVersions => LinkedDocumentVersionItems.AsQueryable();
        public IQueryable<WorkflowTemplate> WorkflowTemplates => WorkflowTemplateItems.AsQueryable();
        public IQueryable<WorkflowTemplatePhase> WorkflowTemplatePhases => WorkflowTemplatePhaseItems.AsQueryable();
        public IQueryable<PromptWorkflow> PromptWorkflows => PromptWorkflowItems.AsQueryable();
        public IQueryable<PromptWorkflowPhase> PromptWorkflowPhases => PromptWorkflowPhaseItems.AsQueryable();
        public IQueryable<PromptWorkflowEvent> PromptWorkflowEvents => PromptWorkflowEventItems.AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatSession> AiChatSessions => Enumerable.Empty<Thoth.Domain.Ai.AiChatSession>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiChatMessage> AiChatMessages => Enumerable.Empty<Thoth.Domain.Ai.AiChatMessage>().AsQueryable();
        public IQueryable<Thoth.Domain.Ai.AiUserSettings> AiUserSettings => Enumerable.Empty<Thoth.Domain.Ai.AiUserSettings>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Notebook> Notebooks => Enumerable.Empty<Thoth.Domain.Notebooks.Notebook>().AsQueryable();
        public IQueryable<Thoth.Domain.Notebooks.Note> Notes => Enumerable.Empty<Thoth.Domain.Notebooks.Note>().AsQueryable();
        public IQueryable<Thoth.Domain.Diagrams.Diagram> Diagrams => Enumerable.Empty<Thoth.Domain.Diagrams.Diagram>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class => Route(entity, add: true);

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities)
            {
                Route(entity, add: true);
            }
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class => Route(entity, add: false);

        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities.ToList())
            {
                Route(entity, add: false);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("Save failed.");
            }

            return Task.FromResult(1);
        }

        private void Route<TEntity>(TEntity entity, bool add) where TEntity : class
        {
            switch (entity)
            {
                case User item: Apply(UserItems, item, add); break;
                case WorkingDirectory item: Apply(WorkingDirectoryItems, item, add); break;
                case Prompt item: Apply(PromptItems, item, add); break;
                case PromptVersion item: Apply(PromptVersionItems, item, add); break;
                case PromptFileReference item: Apply(PromptFileReferenceItems, item, add); break;
                case LinkedDocument item: Apply(LinkedDocumentItems, item, add); break;
                case LinkedDocumentVersion item: Apply(LinkedDocumentVersionItems, item, add); break;
                case WorkflowTemplate item: Apply(WorkflowTemplateItems, item, add); break;
                case WorkflowTemplatePhase item: Apply(WorkflowTemplatePhaseItems, item, add); break;
                case PromptWorkflow item: Apply(PromptWorkflowItems, item, add); break;
                case PromptWorkflowPhase item: Apply(PromptWorkflowPhaseItems, item, add); break;
                case PromptWorkflowEvent item: Apply(PromptWorkflowEventItems, item, add); break;
            }
        }

        private static void Apply<T>(List<T> list, T entity, bool add)
        {
            if (add)
            {
                list.Add(entity);
            }
            else
            {
                list.Remove(entity);
            }
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 6, 1, 12, 30, 0, TimeSpan.Zero);
    }

    private sealed class RecordingWorkflowNotifier : IWorkflowNotifier
    {
        public List<TaskSummaryDto> Changes { get; } = new();

        public Task TaskWorkflowChangedAsync(TaskSummaryDto summary, CancellationToken cancellationToken)
        {
            Changes.Add(summary);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTerminalCoordinator : ITerminalSessionCoordinator
    {
        public (Guid PromptId, string Cwd, string Shell, byte[]? InitialInput, byte[]? FollowUpInput)? LastCreate { get; private set; }

        public Task<TerminalSessionDescriptor> CreateAsync(
            Guid promptId,
            string cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null)
        {
            LastCreate = (promptId, cwd, shell, initialInput, followUpInput);
            return Task.FromResult(new TerminalSessionDescriptor(
                Guid.CreateVersion7(),
                promptId,
                shell,
                cwd,
                DateTimeOffset.UtcNow));
        }

        public Task<TerminalSessionDescriptor> CreateGenericAsync(
            Guid ownerId,
            string? cwd,
            string shell,
            byte[]? initialInput,
            CancellationToken cancellationToken,
            byte[]? followUpInput = null) =>
            throw new NotSupportedException();

        public IReadOnlyList<TerminalSessionDescriptor> ListForOwner(Guid ownerId) =>
            Array.Empty<TerminalSessionDescriptor>();

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

        public IReadOnlyList<TerminalSessionDescriptor> ListAll() =>
            Array.Empty<TerminalSessionDescriptor>();

        public TerminalSessionDescriptor? TryGetSession(Guid sessionId) => null;

        public TerminalOutputHistoryDto? GetOutputHistory(Guid sessionId) => null;

        public Task KillForPromptAsync(Guid promptId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
