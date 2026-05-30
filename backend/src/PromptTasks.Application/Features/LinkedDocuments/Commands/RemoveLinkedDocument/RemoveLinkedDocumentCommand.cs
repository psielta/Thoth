using MediatR;

namespace PromptTasks.Application.Features.LinkedDocuments.Commands.RemoveLinkedDocument;

public sealed record RemoveLinkedDocumentCommand(Guid Id) : IRequest;
