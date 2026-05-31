using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(
    Guid SessionId,
    string Message,
    bool IncludePromptContext,
    string? PromptContent) : IStreamRequest<ChatChunkDto>;
