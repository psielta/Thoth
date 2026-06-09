using MediatR;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Commands.ResumeLinkedDocument;

public sealed record ResumeLinkedDocumentCommand(Guid Id) : IRequest<LinkedDocumentDto>;
