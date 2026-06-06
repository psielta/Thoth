using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.BrowseDirectory;

public sealed record BrowseDirectoryQuery(Guid WorkingDirectoryId, string RelativePath = "")
    : IRequest<IReadOnlyList<DirectoryEntryDto>>;