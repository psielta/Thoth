using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Files.Queries.BrowseDirectory;

public sealed record BrowseDirectoryQuery(Guid WorkingDirectoryId, string RelativePath = "")
    : IRequest<IReadOnlyList<DirectoryEntryDto>>;