using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.UpdateWorkingDirectory;

public sealed record UpdateWorkingDirectoryCommand(
    Guid Id,
    string Name,
    string AbsolutePath,
    bool RespectGitignore) : IRequest<WorkingDirectoryDto>;
