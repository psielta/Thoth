using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentVersions;

public sealed record GetLinkedDocumentVersionsQuery(Guid Id) : IRequest<IReadOnlyList<LinkedDocumentVersionDto>>;
