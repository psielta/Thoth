using MediatR;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.RefreshLinkedDocument;

public sealed record RefreshLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
