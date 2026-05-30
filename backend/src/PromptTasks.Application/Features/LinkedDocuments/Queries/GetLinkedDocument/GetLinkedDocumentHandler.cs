using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;

public sealed class GetLinkedDocumentHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLinkedDocumentQuery, LinkedDocumentDto>
{
    public Task<LinkedDocumentDto> Handle(GetLinkedDocumentQuery request, CancellationToken cancellationToken)
    {
        var (document, _) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        return Task.FromResult(document.ToDto());
    }
}
