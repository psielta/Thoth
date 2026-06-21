using FluentAssertions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.AppSettings.Commands.UpdateAppSettings;
using Thoth.Application.Features.AppSettings.Queries.GetAppSettings;
using Thoth.Domain.Ai;
using Thoth.Domain.AppSettings;
using Thoth.Domain.Diagrams;
using Thoth.Domain.FutureTasks;
using Thoth.Domain.Notebooks;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;
using Thoth.Domain.Workflows;

namespace Thoth.Application.UnitTests;

public sealed class AppSettingsHandlerTests
{
    [Fact]
    public async Task GetAppSettings_returns_default_enabled_when_user_has_no_settings()
    {
        var context = new FakeApplicationDbContext();
        var handler = new GetAppSettingsHandler(context, new FakeCurrentUser());

        var result = await handler.Handle(new GetAppSettingsQuery(), CancellationToken.None);

        result.ShowAgentTerminalOfferAfterChildPrompt.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAppSettings_creates_user_settings()
    {
        var context = new FakeApplicationDbContext();
        var handler = new UpdateAppSettingsHandler(context, new FakeCurrentUser());

        var result = await handler.Handle(new UpdateAppSettingsCommand(false), CancellationToken.None);

        result.ShowAgentTerminalOfferAfterChildPrompt.Should().BeFalse();
        context.AppUserSettingsItems.Should().ContainSingle().Which.ShowAgentTerminalOfferAfterChildPrompt.Should().BeFalse();
        context.AppUserSettingsItems.Single().OwnerId.Should().Be(User.SystemUserId);
        context.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAppSettings_updates_existing_user_settings()
    {
        var context = new FakeApplicationDbContext();
        context.AppUserSettingsItems.Add(new AppUserSettings
        {
            OwnerId = User.SystemUserId,
            ShowAgentTerminalOfferAfterChildPrompt = false,
        });
        var handler = new UpdateAppSettingsHandler(context, new FakeCurrentUser());

        var result = await handler.Handle(new UpdateAppSettingsCommand(true), CancellationToken.None);

        result.ShowAgentTerminalOfferAfterChildPrompt.Should().BeTrue();
        context.AppUserSettingsItems.Should().ContainSingle().Which.ShowAgentTerminalOfferAfterChildPrompt.Should().BeTrue();
        context.SaveChangesCount.Should().Be(1);
    }

    private sealed class FakeApplicationDbContext : IApplicationDbContext
    {
        public List<AppUserSettings> AppUserSettingsItems { get; } = new();
        public IQueryable<User> Users => Enumerable.Empty<User>().AsQueryable();
        public IQueryable<WorkingDirectory> WorkingDirectories => Enumerable.Empty<WorkingDirectory>().AsQueryable();
        public IQueryable<FutureTask> FutureTasks => Enumerable.Empty<FutureTask>().AsQueryable();
        public IQueryable<FutureTaskLabel> FutureTaskLabels => Enumerable.Empty<FutureTaskLabel>().AsQueryable();
        public IQueryable<Prompt> Prompts => Enumerable.Empty<Prompt>().AsQueryable();
        public IQueryable<PromptVersion> PromptVersions => Enumerable.Empty<PromptVersion>().AsQueryable();
        public IQueryable<PromptFileReference> PromptFileReferences => Enumerable.Empty<PromptFileReference>().AsQueryable();
        public IQueryable<LinkedDocument> LinkedDocuments => Enumerable.Empty<LinkedDocument>().AsQueryable();
        public IQueryable<LinkedDocumentVersion> LinkedDocumentVersions => Enumerable.Empty<LinkedDocumentVersion>().AsQueryable();
        public IQueryable<WorkflowTemplate> WorkflowTemplates => Enumerable.Empty<WorkflowTemplate>().AsQueryable();
        public IQueryable<WorkflowTemplatePhase> WorkflowTemplatePhases => Enumerable.Empty<WorkflowTemplatePhase>().AsQueryable();
        public IQueryable<PromptWorkflow> PromptWorkflows => Enumerable.Empty<PromptWorkflow>().AsQueryable();
        public IQueryable<PromptWorkflowPhase> PromptWorkflowPhases => Enumerable.Empty<PromptWorkflowPhase>().AsQueryable();
        public IQueryable<PromptWorkflowEvent> PromptWorkflowEvents => Enumerable.Empty<PromptWorkflowEvent>().AsQueryable();
        public IQueryable<AiChatSession> AiChatSessions => Enumerable.Empty<AiChatSession>().AsQueryable();
        public IQueryable<AiChatMessage> AiChatMessages => Enumerable.Empty<AiChatMessage>().AsQueryable();
        public IQueryable<AiUserSettings> AiUserSettings => Enumerable.Empty<AiUserSettings>().AsQueryable();
        public IQueryable<AppUserSettings> AppUserSettings => AppUserSettingsItems.AsQueryable();
        public IQueryable<Notebook> Notebooks => Enumerable.Empty<Notebook>().AsQueryable();
        public IQueryable<Note> Notes => Enumerable.Empty<Note>().AsQueryable();
        public IQueryable<Diagram> Diagrams => Enumerable.Empty<Diagram>().AsQueryable();
        public int SaveChangesCount { get; private set; }

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is AppUserSettings settings)
            {
                AppUserSettingsItems.Add(settings);
            }
        }

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is AppUserSettings settings)
            {
                AppUserSettingsItems.Remove(settings);
            }
        }

        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities.ToList())
            {
                Remove(entity);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => User.SystemUserId;
    }
}
