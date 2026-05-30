using FluentValidation;

namespace PromptTasks.Application.Features.Files.Queries.ValidateFileReferences;

public sealed class ValidateFileReferencesValidator : AbstractValidator<ValidateFileReferencesQuery>
{
    public ValidateFileReferencesValidator()
    {
        RuleFor(query => query.WorkingDirectoryId).NotEmpty();
        RuleFor(query => query.RelativePaths).NotEmpty().Must(paths => paths.Count <= 100)
            .WithMessage("A maximum of 100 file references can be validated at once.");
        RuleForEach(query => query.RelativePaths).NotEmpty().MaximumLength(512);
    }
}
