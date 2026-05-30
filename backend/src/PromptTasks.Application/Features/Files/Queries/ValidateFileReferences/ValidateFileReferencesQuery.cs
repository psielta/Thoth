using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.Files.Queries.ValidateFileReferences;

public sealed record ValidateFileReferencesQuery(Guid WorkingDirectoryId, IReadOnlyList<string> RelativePaths)
    : IRequest<IReadOnlyList<FileReferenceValidationDto>>;
