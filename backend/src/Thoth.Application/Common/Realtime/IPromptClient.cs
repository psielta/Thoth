using Thoth.Application.Common.Models;

namespace Thoth.Application.Common.Realtime;

public interface IPromptClient
{
    Task PromptCreated(PromptDto prompt);
    Task PromptUpdated(PromptDto prompt);
    Task PromptDeleted(Guid promptId, Guid workingDirectoryId);
    Task LinkedDocumentLinked(LinkedDocumentDto document);
    Task LinkedDocumentUpdated(LinkedDocumentDto document);
    Task LinkedDocumentRemoved(Guid linkedDocumentId, Guid promptId, Guid workingDirectoryId);
    Task TaskWorkflowChanged(TaskSummaryDto summary);
    Task AgentUsageUpdated(AgentUsageDto usage);
    Task WorkspaceFileChanged(Guid workingDirectoryId, string relativePath);
    Task TerminalOutput(Guid sessionId, long startOffset, string dataBase64);
    Task TerminalExited(Guid sessionId, int exitCode);
}
