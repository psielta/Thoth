using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Application.Features.WorkingDirectories.Commands.DeleteWorkingDirectory;

public sealed class DeleteWorkingDirectoryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IGeminiClient gemini,
    ILinkedDocumentWatchCoordinator linkedDocumentWatchCoordinator)
    : IRequestHandler<DeleteWorkingDirectoryCommand>
{
    public async Task Handle(DeleteWorkingDirectoryCommand request, CancellationToken cancellationToken)
    {
        var directory = context.WorkingDirectories
            .FirstOrDefault(item => item.Id == request.Id && item.OwnerId == currentUser.UserId);

        if (directory is null)
        {
            throw new NotFoundException("Working directory was not found.");
        }

        var prompts = context.Prompts
            .Where(prompt => prompt.WorkingDirectoryId == directory.Id)
            .ToList();
        var promptIds = prompts.Select(prompt => prompt.Id).ToHashSet();

        var linkedDocuments = context.LinkedDocuments
            .Where(document => document.WorkingDirectoryId == directory.Id || promptIds.Contains(document.PromptId))
            .ToList();
        var linkedDocumentIds = linkedDocuments.Select(document => document.Id).ToHashSet();
        foreach (var document in linkedDocuments)
        {
            linkedDocumentWatchCoordinator.StopTracking(document.Id);
        }
        Remove(context.LinkedDocumentVersions
            .Where(version => linkedDocumentIds.Contains(version.LinkedDocumentId))
            .ToList());
        Remove(linkedDocuments);

        var workflows = context.PromptWorkflows
            .Where(workflow => promptIds.Contains(workflow.PromptId))
            .ToList();
        var workflowIds = workflows.Select(workflow => workflow.Id).ToHashSet();
        Remove(context.PromptWorkflowEvents
            .Where(@event => workflowIds.Contains(@event.PromptWorkflowId))
            .ToList());
        Remove(context.PromptWorkflowPhases
            .Where(phase => workflowIds.Contains(phase.PromptWorkflowId))
            .ToList());
        Remove(workflows);

        Remove(context.PromptFileReferences
            .Where(reference => promptIds.Contains(reference.PromptId))
            .ToList());
        Remove(context.PromptVersions
            .Where(version => promptIds.Contains(version.PromptId))
            .ToList());

        var chatSessions = context.AiChatSessions
            .Where(session =>
                session.WorkingDirectoryId == directory.Id ||
                (session.PromptId.HasValue && promptIds.Contains(session.PromptId.Value)))
            .ToList();
        var cacheNames = chatSessions
            .Select(session => session.GeminiCacheName)
            .Where(cacheName => !string.IsNullOrWhiteSpace(cacheName))
            .Distinct()
            .ToList();
        var chatSessionIds = chatSessions.Select(session => session.Id).ToHashSet();
        Remove(context.AiChatMessages
            .Where(message => chatSessionIds.Contains(message.SessionId))
            .ToList());
        Remove(chatSessions);

        var notebooks = context.Notebooks
            .Where(notebook => notebook.WorkingDirectoryId == directory.Id)
            .ToList();
        var notebookIds = notebooks.Select(notebook => notebook.Id).ToHashSet();
        Remove(context.Notes
            .Where(note => notebookIds.Contains(note.NotebookId))
            .ToList());
        Remove(notebooks);

        var futureTasks = context.FutureTasks
            .Where(task => task.WorkingDirectoryId == directory.Id)
            .ToList();
        var futureTaskIds = futureTasks.Select(task => task.Id).ToHashSet();
        Remove(context.FutureTaskLabels
            .Where(label => futureTaskIds.Contains(label.FutureTaskId))
            .ToList());
        Remove(futureTasks);

        Remove(context.Diagrams
            .Where(diagram => diagram.WorkingDirectoryId == directory.Id)
            .ToList());

        var promptRoots = prompts
            .Where(prompt => !prompt.ParentPromptId.HasValue || !promptIds.Contains(prompt.ParentPromptId.Value))
            .ToList();
        Remove(promptRoots);

        context.Remove(directory);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var cacheName in cacheNames)
        {
            try { await gemini.DeleteCacheAsync(cacheName!, cancellationToken); }
            catch { /* best effort */ }
        }
    }

    private void Remove<TEntity>(IReadOnlyCollection<TEntity> entities)
        where TEntity : class
    {
        if (entities.Count > 0)
        {
            context.RemoveRange(entities);
        }
    }
}
