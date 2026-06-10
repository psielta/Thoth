using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Git.Queries.GetGitDiff;

public sealed record GetGitDiffQuery(Guid WorkingDirectoryId, string Path)
    : IRequest<GitDiffDto>;
