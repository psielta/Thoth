using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Interfaces;

public interface IPromptNotifier
{
    Task PromptCreatedAsync(PromptDto prompt, CancellationToken cancellationToken);
    Task PromptUpdatedAsync(PromptDto prompt, CancellationToken cancellationToken);
    Task PromptDeletedAsync(Guid promptId, Guid workingDirectoryId, CancellationToken cancellationToken);
}
