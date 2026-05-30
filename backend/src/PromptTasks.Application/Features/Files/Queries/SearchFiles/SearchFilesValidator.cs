using FluentValidation;

namespace PromptTasks.Application.Features.Files.Queries.SearchFiles;

public sealed class SearchFilesValidator : AbstractValidator<SearchFilesQuery>
{
    public SearchFilesValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.Query).MaximumLength(256);
        RuleFor(query => query.Limit).InclusiveBetween(1, 200);
    }
}
