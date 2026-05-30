using FluentValidation;

namespace PromptTasks.Application.Features.WorkingDirectories.Queries.ValidateWorkingDirectoryPath;

public sealed class ValidateWorkingDirectoryPathValidator : AbstractValidator<ValidateWorkingDirectoryPathQuery>
{
    public ValidateWorkingDirectoryPathValidator()
    {
        RuleFor(query => query.AbsolutePath).NotEmpty().MaximumLength(1024);
    }
}
