using MediatR;

namespace Thoth.Application.Features.Prompts.Commands.DeletePrompt;

public sealed record DeletePromptCommand(Guid Id) : IRequest;
