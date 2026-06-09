using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.GenerateNoteMarkdown;

public sealed record GenerateNoteMarkdownCommand(
    string Instruction,
    string? Format,
    string Model,
    double Temperature,
    GeminiThinking Thinking,
    Guid? NotebookId,
    string? CurrentContent) : IRequest<GeneratedNoteDto>;
