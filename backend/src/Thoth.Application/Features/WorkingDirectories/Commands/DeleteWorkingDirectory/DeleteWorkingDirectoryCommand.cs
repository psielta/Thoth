using MediatR;

namespace Thoth.Application.Features.WorkingDirectories.Commands.DeleteWorkingDirectory;

public sealed record DeleteWorkingDirectoryCommand(Guid Id) : IRequest;
