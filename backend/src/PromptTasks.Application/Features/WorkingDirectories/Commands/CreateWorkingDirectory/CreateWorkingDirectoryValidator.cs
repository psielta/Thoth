using FluentValidation;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.CreateWorkingDirectory;

public sealed class CreateWorkingDirectoryValidator : AbstractValidator<CreateWorkingDirectoryCommand>
{
    public CreateWorkingDirectoryValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.AbsolutePath).NotEmpty().MaximumLength(1024);
    }
}
