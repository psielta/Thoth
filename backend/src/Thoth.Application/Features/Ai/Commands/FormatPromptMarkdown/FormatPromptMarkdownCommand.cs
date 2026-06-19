using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.FormatPromptMarkdown;

public sealed record FormatPromptMarkdownCommand(
    string Content,
    string Model,
    double Temperature,
    GeminiThinking Thinking) : IRequest<FormattedPromptMarkdownDto>;