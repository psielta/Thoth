using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;

public sealed record GetLinkedDocumentQuery(Guid Id) : IRequest<LinkedDocumentDto>;
