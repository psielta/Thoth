using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.ReadFileContent;

public sealed record ReadFileContentQuery(Guid WorkingDirectoryId, string RelativePath)
    : IRequest<FileContentDto>;