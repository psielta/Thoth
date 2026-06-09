using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentVersions;

public sealed record GetLinkedDocumentVersionsQuery(Guid Id) : IRequest<IReadOnlyList<LinkedDocumentVersionDto>>;
