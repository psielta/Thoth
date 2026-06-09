using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.UpdateAiSettings;

public sealed record UpdateAiSettingsCommand(
    string Model,
    double Temperature,
    bool ThinkingEnabled,
    int? ThinkingBudget,
    string? ThinkingLevel) : IRequest<AiSettingsDto>;
