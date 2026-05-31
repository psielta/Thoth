using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.AddWorkflowNote;

public sealed class AddWorkflowNoteValidator : AbstractValidator<AddWorkflowNoteCommand>
{
    public AddWorkflowNoteValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.Note).NotEmpty().MaximumLength(4000);
    }
}
