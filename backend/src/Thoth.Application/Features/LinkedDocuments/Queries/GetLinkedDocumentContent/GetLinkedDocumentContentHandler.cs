using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentContent;

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
