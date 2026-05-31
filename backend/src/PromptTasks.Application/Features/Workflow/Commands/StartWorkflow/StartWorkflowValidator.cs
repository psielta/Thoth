using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.StartWorkflow;

public sealed class StartWorkflowValidator : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowValidator()
    {
        RuleFor(command => command.PromptId).NotEmpty();
        RuleFor(command => command.InitialPhaseOrderIndex)
            .GreaterThanOrEqualTo(0)
            .When(command => command.InitialPhaseOrderIndex.HasValue);
    }
}
