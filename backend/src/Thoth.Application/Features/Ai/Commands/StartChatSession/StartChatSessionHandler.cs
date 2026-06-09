using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Features.Ai.Models;
using Thoth.Domain.Ai;

namespace Thoth.Application.Features.Ai.Commands.StartChatSession;

public sealed class StartChatSessionHandler(
    IApplicationDbContext context,
    IGeminiModelCatalog catalog,
    ICurrentUser currentUser)
    : IRequestHandler<StartChatSessionCommand, AiChatSessionDto>
{
    public async Task<AiChatSessionDto> Handle(StartChatSessionCommand request, CancellationToken cancellationToken)
    {
        if (catalog.GetModel(request.Model) is null)
            throw new NotFoundException($"Modelo '{request.Model}' não encontrado.");

        var session = new AiChatSession
        {
            OwnerId = currentUser.UserId,
            WorkingDirectoryId = request.WorkingDirectoryId,
            PromptId = request.PromptId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Nova sessão" : request.Title,
            Model = request.Model,
            Temperature = request.Temperature,
            ThinkingEnabled = request.ThinkingEnabled,
            ThinkingBudget = request.ThinkingBudget,
            ThinkingLevel = request.ThinkingLevel,
        };

        context.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        return new AiChatSessionDto(
            session.Id,
            session.WorkingDirectoryId,
            session.PromptId,
            session.Title,
            session.Model,
            session.Temperature,
            session.ThinkingEnabled,
            session.ThinkingBudget,
            session.ThinkingLevel,
            session.CreatedAtUtc,
            new List<AiChatMessageDto>());
    }
}
