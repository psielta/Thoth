using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Interfaces;

public interface IPromptNotifier
{
    Task PromptCreatedAsync(PromptDto prompt, CancellationToken cancellationToken);
    Task PromptUpdatedAsync(PromptDto prompt, CancellationToken cancellationToken);
    Task PromptDeletedAsync(Guid promptId, Guid workingDirectoryId, CancellationToken cancellationToken);
}
