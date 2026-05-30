using MediatR;
using PromptTasks.Application.Common.Models;
using PromptTasks.Domain.Prompts;

namespace PromptTasks.Application.Features.Prompts.Commands.UpdatePromptStatus;

public sealed record UpdatePromptStatusCommand(Guid Id, PromptStatus Status, string RowVersion) : IRequest<PromptDto>;
