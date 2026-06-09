using FluentValidation;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocumentVersions;

public sealed class GetLinkedDocumentVersionsValidator : AbstractValidator<GetLinkedDocumentVersionsQuery>
{
    public GetLinkedDocumentVersionsValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
