using MediatR;

namespace Thoth.Application.Features.WorkingDirectories.Commands.OpenWorkingDirectoryInVsCode;

public sealed record OpenWorkingDirectoryInVsCodeCommand(Guid Id) : IRequest;
