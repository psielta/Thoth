using FluentValidation;

namespace Thoth.Application.Features.Files.Queries.BrowseDirectory;

public sealed class BrowseDirectoryValidator : AbstractValidator<BrowseDirectoryQuery>
{
    public BrowseDirectoryValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.RelativePath).MaximumLength(1024);
    }
}