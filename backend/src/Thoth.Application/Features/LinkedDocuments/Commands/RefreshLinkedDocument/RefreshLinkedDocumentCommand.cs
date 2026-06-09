using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Commands.RefreshLinkedDocument;

public sealed record RefreshLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
