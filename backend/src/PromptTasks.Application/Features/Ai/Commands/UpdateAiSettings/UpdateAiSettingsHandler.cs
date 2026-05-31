using MediatR;
using PromptTasks.Application.Common.Exceptions;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Features.Ai.Models;
using PromptTasks.Domain.Ai;

namespace PromptTasks.Application.Features.Ai.Commands.UpdateAiSettings;

public sealed class UpdateAiSettingsHandler(
    IApplicationDbContext context,
    IGeminiModelCatalog catalog,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateAiSettingsCommand, AiSettingsDto>
{
    public async Task<AiSettingsDto> Handle(UpdateAiSettingsCommand request, CancellationToken cancellationToken)
    {
        if (catalog.GetModel(request.Model) is null)
            throw new NotFoundException($"Modelo '{request.Model}' não encontrado na lista curada.");

        var settings = context.AiUserSettings.FirstOrDefault(s => s.OwnerId == currentUser.UserId);
        if (settings is null)
        {
            settings = new AiUserSettings
            {
                OwnerId = currentUser.UserId,
                Model = request.Model,
                Temperature = request.Temperature,
                ThinkingEnabled = request.ThinkingEnabled,
                ThinkingBudget = request.ThinkingBudget,
                ThinkingLevel = request.ThinkingLevel,
            };
            context.Add(settings);
        }
        else
        {
            settings.Model = request.Model;
            settings.Temperature = request.Temperature;
            settings.ThinkingEnabled = request.ThinkingEnabled;
            settings.ThinkingBudget = request.ThinkingBudget;
            settings.ThinkingLevel = request.ThinkingLevel;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new AiSettingsDto(
            settings.Model,
            settings.Temperature,
            settings.ThinkingEnabled,
            settings.ThinkingBudget,
            settings.ThinkingLevel);
    }
}
