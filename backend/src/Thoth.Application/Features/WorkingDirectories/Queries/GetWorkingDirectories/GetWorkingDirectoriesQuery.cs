using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Queries.GetWorkingDirectories;

public sealed record GetWorkingDirectoriesQuery : IRequest<IReadOnlyList<WorkingDirectoryDto>>;
