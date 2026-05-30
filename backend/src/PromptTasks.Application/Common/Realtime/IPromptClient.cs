using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Common.Realtime;

public interface IPromptClient
{
    Task PromptCreated(PromptDto prompt);
    Task PromptUpdated(PromptDto prompt);
    Task PromptDeleted(Guid promptId, Guid workingDirectoryId);
}
