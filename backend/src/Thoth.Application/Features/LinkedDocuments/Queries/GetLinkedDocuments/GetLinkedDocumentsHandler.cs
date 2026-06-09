using MediatR;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Mappings;
using Thoth.Application.Common.Models;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocuments;

public sealed class GetLinkedDocumentsHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetLinkedDocumentsQuery, IReadOnlyList<LinkedDocumentDto>>
{
    public Task<IReadOnlyList<LinkedDocumentDto>> Handle(GetLinkedDocumentsQuery request, CancellationToken cancellationToken)
    {
        var prompt = LinkedDocumentHelpers.GetPrompt(context, request.PromptId, currentUser.UserId);

        IReadOnlyList<LinkedDocumentDto> result = context.LinkedDocuments
            .Where(document => document.PromptId == prompt.Id)
            .OrderBy(document => document.DisplayName ?? document.AbsolutePath)
            .Select(document => document.ToDto())
            .ToList();

        return Task.FromResult(result);
    }
}
