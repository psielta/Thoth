using FluentValidation;

namespace PromptTasks.Application.Features.WorkingDirectories.Commands.UpdateWorkingDirectory;

public sealed class UpdateWorkingDirectoryValidator : AbstractValidator<UpdateWorkingDirectoryCommand>
{
    public UpdateWorkingDirectoryValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.AbsolutePath).NotEmpty().MaximumLength(1024);
    }
}
