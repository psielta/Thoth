using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.RefinePrompt;

public sealed record RefinePromptCommand(
    string Content,
    string Model,
    double Temperature,
    GeminiThinking Thinking,
    Guid? WorkingDirectoryId,
    IReadOnlyList<string> ContextFiles,
    string? CustomInstructions) : IRequest<RefinedPromptDto>;
