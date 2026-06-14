using FluentAssertions;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.WorkingDirectories.Commands.OpenWorkingDirectoryInVsCode;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;

namespace Thoth.Application.UnitTests;

public sealed class OpenWorkingDirectoryInVsCodeHandlerTests
{
    [Fact]
    public async Task Handle_opens_the_owned_workspace_path()
    {
        var context = new FakeApplicationDbContext();
        var workspace = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = "repo",
            AbsolutePath = "D:/repo",
            OwnerId = User.SystemUserId
        };
        context.WorkingDirectoryItems.Add(workspace);
        var launcher = new RecordingWorkspaceEditorLauncher();
        var handler = new OpenWorkingDirectoryInVsCodeHandler(context, new FakeCurrentUser(), launcher);

        await handler.Handle(new OpenWorkingDirectoryInVsCodeCommand(workspace.Id), CancellationToken.None);

        launcher.OpenedPath.Should().Be(workspace.AbsolutePath);
    }

    [Fact]
    public async Task Handle_rejects_workspaces_from_other_users()
    {
        var context = new FakeApplicationDbContext();
        var workspace = new WorkingDirectory
        {
            Id = Guid.CreateVersion7(),
            Name = "other",
            AbsolutePath = "D:/other",
            OwnerId = Guid.CreateVersion7()
        };
        context.WorkingDirectoryItems.Add(workspace);
        var launcher = new RecordingWorkspaceEditorLauncher();
        var handler = new OpenWorkingDirectoryInVsCodeHandler(context, new FakeCurrentUser(), launcher);

        var act = () => handler.Handle(new OpenWorkingDirectoryInVsCodeCommand(workspace.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        launcher.OpenedPath.Should().BeNull();
    }

    private sealed class RecordingWorkspaceEditorLauncher : IWorkspaceEditorLauncher
    {
        public string? OpenedPath { get; private set; }

        public Task OpenVsCodeAsync(string absolutePath, CancellationToken cancellationToken)
        {
            OpenedPath = absolutePath;
            return Task.CompletedTask;
        }

        public Task OpenFileInVsCodeAsync(string workspaceAbsolutePath, string fileAbsolutePath, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
}
