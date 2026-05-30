using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;

public sealed record GetLinkedDocumentContentQuery(Guid Id, int? VersionNumber) : IRequest<LinkedDocumentContentDto>;
