using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocuments;

public sealed record GetLinkedDocumentsQuery(Guid PromptId) : IRequest<IReadOnlyList<LinkedDocumentDto>>;
