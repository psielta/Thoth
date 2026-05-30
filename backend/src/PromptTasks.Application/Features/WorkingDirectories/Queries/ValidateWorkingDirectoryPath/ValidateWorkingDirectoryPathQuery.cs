using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.ValidateWorkingDirectoryPath;

public sealed record ValidateWorkingDirectoryPathQuery(string AbsolutePath) : IRequest<ValidatePathResponse>;
