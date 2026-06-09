using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;

public sealed record GetLinkedDocumentContentQuery(Guid Id, int? VersionNumber) : IRequest<LinkedDocumentContentDto>;
