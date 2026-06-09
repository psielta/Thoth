using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.WorkingDirectories.Queries.GetWorkingDirectory;

public sealed record GetWorkingDirectoryQuery(Guid Id) : IRequest<WorkingDirectoryDto>;
