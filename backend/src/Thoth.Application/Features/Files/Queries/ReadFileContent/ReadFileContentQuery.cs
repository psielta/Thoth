using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Files.Queries.ReadFileContent;

public sealed record ReadFileContentQuery(Guid WorkingDirectoryId, string RelativePath)
    : IRequest<FileContentDto>;