using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Commands.UpdateWorkingDirectory;

public sealed record UpdateWorkingDirectoryCommand(
    Guid Id,
    string Name,
    string AbsolutePath,
    bool RespectGitignore,
    bool EnableAiContext,
    string? TaskNumberPattern = null) : IRequest<WorkingDirectoryDto>;
