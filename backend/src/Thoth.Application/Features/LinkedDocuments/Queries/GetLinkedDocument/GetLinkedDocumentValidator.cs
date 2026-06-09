using FluentValidation;

namespace Thoth.Application.Features.LinkedDocuments.Queries.GetLinkedDocument;

public sealed class GetLinkedDocumentValidator : AbstractValidator<GetLinkedDocumentQuery>
{
    public GetLinkedDocumentValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
