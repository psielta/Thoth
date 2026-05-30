using PromptTasks.Domain.Prompts;
using PromptTasks.Domain.Users;
using PromptTasks.Domain.WorkingDirectories;

namespace PromptTasks.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<WorkingDirectory> WorkingDirectories { get; }
    IQueryable<Prompt> Prompts { get; }
    IQueryable<PromptVersion> PromptVersions { get; }
    IQueryable<PromptFileReference> PromptFileReferences { get; }
    IQueryable<LinkedDocument> LinkedDocuments { get; }
    IQueryable<LinkedDocumentVersion> LinkedDocumentVersions { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
