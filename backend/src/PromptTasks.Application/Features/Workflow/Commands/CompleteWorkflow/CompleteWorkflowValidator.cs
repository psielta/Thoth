using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.CompleteWorkflow;

public sealed class CompleteWorkflowValidator : AbstractValidator<CompleteWorkflowCommand>
{
    public CompleteWorkflowValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.RowVersion).NotEmpty();
        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
