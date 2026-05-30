using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocuments;

public sealed record GetLinkedDocumentsQuery(Guid PromptId) : IRequest<IReadOnlyList<LinkedDocumentDto>>;
