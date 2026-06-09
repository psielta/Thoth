using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.Prompts.Commands.UpdatePromptStatus;

public sealed record UpdatePromptStatusCommand(Guid Id, PromptStatus Status, string RowVersion) : IRequest<PromptDto>;
