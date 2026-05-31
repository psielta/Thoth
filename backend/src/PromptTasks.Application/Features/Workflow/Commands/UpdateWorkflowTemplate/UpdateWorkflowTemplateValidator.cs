using FluentValidation;

namespace PromptTasks.Application.Features.Workflow.Commands.UpdateWorkflowTemplate;

public sealed class UpdateWorkflowTemplateValidator : AbstractValidator<UpdateWorkflowTemplateCommand>
{
    public UpdateWorkflowTemplateValidator()
    {
        RuleFor(command => command.Phases).NotEmpty();
        RuleFor(command => command.Phases)
            .Must(WorkflowPhaseInputValidation.HasContiguousOrder)
            .WithMessage(WorkflowPhaseInputValidation.OrderMessage)
            .When(command => command.Phases is { Count: > 0 });
        RuleForEach(command => command.Phases).ChildRules(phase =>
        {
            phase.RuleFor(item => item.Name).NotEmpty().MaximumLength(120);
            phase.RuleFor(item => item.DefaultActor).IsInEnum();
            phase.RuleFor(item => item.Color)
                .Must(WorkflowPhaseInputValidation.IsValidColor)
                .WithMessage(WorkflowPhaseInputValidation.ColorMessage);
        });
    }
}
