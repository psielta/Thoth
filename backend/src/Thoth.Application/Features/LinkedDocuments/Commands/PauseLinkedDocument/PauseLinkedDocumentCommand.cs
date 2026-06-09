using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Commands.PauseLinkedDocument;

public sealed record PauseLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
