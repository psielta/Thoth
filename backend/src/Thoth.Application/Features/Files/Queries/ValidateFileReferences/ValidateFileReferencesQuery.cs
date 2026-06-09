using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.Files.Queries.ValidateFileReferences;

public sealed record ValidateFileReferencesQuery(Guid WorkingDirectoryId, IReadOnlyList<string> RelativePaths)
    : IRequest<IReadOnlyList<FileReferenceValidationDto>>;
