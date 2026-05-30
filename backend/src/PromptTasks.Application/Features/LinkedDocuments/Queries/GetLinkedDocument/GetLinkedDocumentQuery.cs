using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;

public sealed record GetLinkedDocumentQuery(Guid Id) : IRequest<LinkedDocumentDto>;
