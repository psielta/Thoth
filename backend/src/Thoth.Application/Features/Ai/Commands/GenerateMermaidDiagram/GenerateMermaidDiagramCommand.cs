using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.GenerateMermaidDiagram;

public sealed record GenerateMermaidDiagramCommand(
    string Instruction,
    string? DiagramKind,
    string Model,
    double Temperature,
    GeminiThinking Thinking,
    Guid? WorkingDirectoryId,
    Guid? DiagramId,
    string? CurrentCode) : IRequest<GeneratedMermaidDto>;
