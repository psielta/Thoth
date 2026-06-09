using MediatR;

namespace Thoth.Application.Features.LinkedDocuments.Commands.RemoveLinkedDocument;

public sealed record RemoveLinkedDocumentCommand(Guid Id) : IRequest;
