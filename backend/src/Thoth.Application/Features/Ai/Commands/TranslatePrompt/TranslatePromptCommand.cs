using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.TranslatePrompt;

public sealed record TranslatePromptCommand(
    string Content,
    string Model,
    double Temperature,
    GeminiThinking Thinking) : IRequest<RefinedPromptDto>;
