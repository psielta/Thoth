using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Commands.StartChatSession;

public sealed record StartChatSessionCommand(
    string? Title,
    Guid? WorkingDirectoryId,
    Guid? PromptId,
    string Model,
    double Temperature,
    bool ThinkingEnabled,
    int? ThinkingBudget,
    string? ThinkingLevel) : IRequest<AiChatSessionDto>;
