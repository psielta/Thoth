using Thoth.Domain.Ai;
using Thoth.Domain.AppSettings;
using Thoth.Domain.Diagrams;
using Thoth.Domain.FutureTasks;
using Thoth.Domain.Notebooks;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;
using Thoth.Domain.WorkingDirectories;
using Thoth.Domain.Workflows;

namespace Thoth.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<WorkingDirectory> WorkingDirectories { get; }
    IQueryable<FutureTask> FutureTasks { get; }
    IQueryable<FutureTaskLabel> FutureTaskLabels { get; }
    IQueryable<Prompt> Prompts { get; }
    IQueryable<PromptVersion> PromptVersions { get; }
    IQueryable<PromptFileReference> PromptFileReferences { get; }
    IQueryable<LinkedDocument> LinkedDocuments { get; }
    IQueryable<LinkedDocumentVersion> LinkedDocumentVersions { get; }
    IQueryable<WorkflowTemplate> WorkflowTemplates { get; }
    IQueryable<WorkflowTemplatePhase> WorkflowTemplatePhases { get; }
    IQueryable<PromptWorkflow> PromptWorkflows { get; }
    IQueryable<PromptWorkflowPhase> PromptWorkflowPhases { get; }
    IQueryable<PromptWorkflowEvent> PromptWorkflowEvents { get; }
    IQueryable<AiChatSession> AiChatSessions { get; }
    IQueryable<AiChatMessage> AiChatMessages { get; }
    IQueryable<AiUserSettings> AiUserSettings { get; }
    IQueryable<AppUserSettings> AppUserSettings => Enumerable.Empty<AppUserSettings>().AsQueryable();
    IQueryable<Notebook> Notebooks { get; }
    IQueryable<Note> Notes { get; }
    IQueryable<Diagram> Diagrams { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
