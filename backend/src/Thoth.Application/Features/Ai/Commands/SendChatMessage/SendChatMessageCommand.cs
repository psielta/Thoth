using MediatR;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(
    Guid SessionId,
    string Message,
    bool IncludePromptContext,
    string? PromptContent) : IStreamRequest<ChatChunkDto>;
