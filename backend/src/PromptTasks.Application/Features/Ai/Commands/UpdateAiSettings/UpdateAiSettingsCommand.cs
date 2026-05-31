using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Commands.UpdateAiSettings;

public sealed record UpdateAiSettingsCommand(
    string Model,
    double Temperature,
    bool ThinkingEnabled,
    int? ThinkingBudget,
    string? ThinkingLevel) : IRequest<AiSettingsDto>;
