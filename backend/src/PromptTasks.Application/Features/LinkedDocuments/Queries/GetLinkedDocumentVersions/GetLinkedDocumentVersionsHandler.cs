using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentVersions;

public sealed class GetLinkedDocumentVersionsHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLinkedDocumentVersionsQuery, IReadOnlyList<LinkedDocumentVersionDto>>
{
    public Task<IReadOnlyList<LinkedDocumentVersionDto>> Handle(
        GetLinkedDocumentVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var (document, _) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);

        IReadOnlyList<LinkedDocumentVersionDto> result = context.LinkedDocumentVersions
            .Where(version => version.LinkedDocumentId == document.Id)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => version.ToDto())
            .ToList();

        return Task.FromResult(result);
    }
}
