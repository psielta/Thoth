using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.GetWorkingDirectories;

public sealed record GetWorkingDirectoriesQuery : IRequest<IReadOnlyList<WorkingDirectoryDto>>;
