using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetOriginalFileContent;

public sealed record GetOriginalFileContentQuery(Guid WorkingDirectoryId, string Path)
    : IRequest<GitOriginalFileDto>;
