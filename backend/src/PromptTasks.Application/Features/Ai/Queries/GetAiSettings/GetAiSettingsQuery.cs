using MediatR;
using PromptTasks.Application.Features.Ai.Models;

namespace PromptTasks.Application.Features.Ai.Queries.GetAiSettings;

public sealed record GetAiSettingsQuery : IRequest<AiSettingsDto>;
