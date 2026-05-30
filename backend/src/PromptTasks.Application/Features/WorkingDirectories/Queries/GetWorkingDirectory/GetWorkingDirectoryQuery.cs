using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.GetWorkingDirectory;

public sealed record GetWorkingDirectoryQuery(Guid Id) : IRequest<WorkingDirectoryDto>;
