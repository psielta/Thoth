using MediatR;

namespace PromptTasks.Application.Features.Prompts.Commands.DeletePrompt;

public sealed record DeletePromptCommand(Guid Id) : IRequest;
