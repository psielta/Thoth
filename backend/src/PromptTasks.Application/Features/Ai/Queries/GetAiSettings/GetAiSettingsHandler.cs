using MediatR;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.GetAiSettings;

public sealed class GetAiSettingsHandler(
    IApplicationDbContext context,
    IGeminiModelCatalog catalog,
    ICurrentUser currentUser)
    : IRequestHandler<GetAiSettingsQuery, AiSettingsDto>
{
    public Task<AiSettingsDto> Handle(GetAiSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = context.AiUserSettings.FirstOrDefault(s => s.OwnerId == currentUser.UserId);
        if (settings is null)
        {
            var defaultModel = catalog.GetModels().FirstOrDefault()?.Id ?? "gemini-3.5-flash";
            return Task.FromResult(new AiSettingsDto(defaultModel, 0.7, true, null, "high"));
        }

        return Task.FromResult(new AiSettingsDto(
            settings.Model,
            settings.Temperature,
            settings.ThinkingEnabled,
            settings.ThinkingBudget,
            settings.ThinkingLevel));
    }
}
