using MediatR;

namespace Thoth.Application.Features.Files.Commands.OpenFileInVsCode;

public sealed record OpenFileInVsCodeCommand(Guid WorkingDirectoryId, string RelativePath) : IRequest;
