using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.SearchFiles;

public sealed record SearchFilesQuery(Guid WorkingDirectoryId, string Query, int Limit = 50)
    : IRequest<IReadOnlyList<FileSearchResultDto>>;
