using FluentValidation;

namespace Thoth.Application.Features.Prompts.Commands.ReorderBoardColumn;

public sealed class ReorderBoardColumnValidator : AbstractValidator<ReorderBoardColumnCommand>
{
    public ReorderBoardColumnValidator()
    {
        RuleFor(command => command.OrderedPromptIds)
            .NotEmpty()
            .Must(ids => ids is not null && ids.All(id => id != Guid.Empty))
            .WithMessage("Prompt ids must not contain empty values.")
            .Must(ids => ids is not null && ids.Distinct().Count() == ids.Count)
            .WithMessage("Prompt ids must not contain duplicates.");
    }
}
