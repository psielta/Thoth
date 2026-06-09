using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.StartChatSession;

public sealed record StartChatSessionCommand(
    string? Title,
    Guid? WorkingDirectoryId,
    Guid? PromptId,
    string Model,
    double Temperature,
    bool ThinkingEnabled,
    int? ThinkingBudget,
    string? ThinkingLevel) : IRequest<AiChatSessionDto>;
