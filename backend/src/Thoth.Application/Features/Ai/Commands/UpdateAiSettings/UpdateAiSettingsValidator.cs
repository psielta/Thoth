using FluentValidation;

namespace Thoth.Application.Features.Ai.Commands.UpdateAiSettings;

public sealed class UpdateAiSettingsValidator : AbstractValidator<UpdateAiSettingsCommand>
{
    public UpdateAiSettingsValidator()
    {
        RuleFor(c => c.Model).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Temperature).InclusiveBetween(0.0, 2.0);
    }
}
