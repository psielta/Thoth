using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Common.Mappings;
using PromptTasks.Application.Common.Models;

namespace PromptTasks.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;

public sealed class GetLinkedDocumentContentHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLinkedDocumentContentQuery, LinkedDocumentContentDto>
{
    public Task<LinkedDocumentContentDto> Handle(GetLinkedDocumentContentQuery request, CancellationToken cancellationToken)
    {
        var (document, _) = LinkedDocumentHelpers.GetDocument(context, request.Id, currentUser.UserId);
        var versions = context.LinkedDocumentVersions.Where(version => version.LinkedDocumentId == document.Id);
        var version = request.VersionNumber.HasValue
            ? versions.FirstOrDefault(item => item.VersionNumber == request.VersionNumber.Value)
            : versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();

        if (version is null)
        {
            throw new NotFoundException("Linked document content was not found.");
        }

        return Task.FromResult(version.ToContentDto());
    }
}
