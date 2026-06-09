using MediatR;
using Thoth.Application.Common.Models;
using Thoth.Domain.Prompts;

namespace Thoth.Application.Features.PromptTemplates.Commands.GeneratePromptDraft;

public sealed record GeneratePromptDraftCommand(
    Guid LinkedDocumentId,
    PromptTemplateKey TemplateKey,
    string? PullRequest = null,
    IReadOnlyDictionary<string, string>? Inputs = null)
    : IRequest<GeneratedPromptDraftDto>;
