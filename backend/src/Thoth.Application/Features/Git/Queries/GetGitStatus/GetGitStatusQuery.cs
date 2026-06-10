using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetGitStatus;

public sealed record GetGitStatusQuery(Guid WorkingDirectoryId)
    : IRequest<IReadOnlyList<GitFileStatusDto>>;
