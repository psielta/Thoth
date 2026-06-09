using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Files.Queries.SearchFiles;

public sealed record SearchFilesQuery(Guid WorkingDirectoryId, string Query, int Limit = 50)
    : IRequest<IReadOnlyList<FileSearchResultDto>>;
