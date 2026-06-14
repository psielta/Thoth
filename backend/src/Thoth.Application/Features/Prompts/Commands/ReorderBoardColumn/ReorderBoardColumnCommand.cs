using MediatR;

namespace Thoth.Application.Features.Prompts.Commands.ReorderBoardColumn;

public sealed record ReorderBoardColumnCommand(IReadOnlyList<Guid> OrderedPromptIds) : IRequest;
