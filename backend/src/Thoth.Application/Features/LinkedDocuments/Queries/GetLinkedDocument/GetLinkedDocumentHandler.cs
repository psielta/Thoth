using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;

public sealed class GetLinkedDocumentHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLinkedDocumentQuery, LinkedDocumentDto>
{
    public Task<LinkedDocumentDto> Handle(GetLinkedDocumentQuery request, CancellationToken cancellationToken)
    {
        var (document, _) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        return Task.FromResult(document.ToDto());
    }
}
