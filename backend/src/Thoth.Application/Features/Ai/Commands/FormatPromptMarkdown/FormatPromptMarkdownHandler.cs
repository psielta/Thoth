using MediatR;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Interfaces;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Models;

namespace Thoth.Application.Features.Ai.Commands.FormatPromptMarkdown;

public sealed class FormatPromptMarkdownHandler(
    IGeminiClient gemini,
    IGeminiModelCatalog catalog)
    : IRequestHandler<FormatPromptMarkdownCommand, FormattedPromptMarkdownDto>
{
    private const string FormatSystemInstruction =
        "Você é um assistente que formata texto em Markdown bem estruturado para um editor TipTap. " +
        "Converta o conteúdo do usuário em Markdown limpo e organizado (títulos ##/###, listas, negrito, " +
        "itálico e code blocks com linguagem quando aplicável). " +
        "Preserve o idioma e o significado do texto original — não reescreva, não resuma e não adicione conteúdo novo. " +
        "Preserve menções @caminho/arquivo intactas. " +
        "Responda APENAS com o Markdown formatado. " +
        "NÃO envolva todo o conteúdo em cercas de código. " +
        "Não adicione comentários ou explicações fora do conteúdo.";

    public async Task<FormattedPromptMarkdownDto> Handle(
        FormatPromptMarkdownCommand request,
        CancellationToken cancellationToken)
    {
        if (catalog.GetModel(request.Model) is null)
            throw new NotFoundException($"Modelo '{request.Model}' não encontrado.");

        var geminiRequest = new GeminiGenerationRequest(
            Model: request.Model,
            Temperature: request.Temperature,
            Thinking: request.Thinking,
            IncludeThoughts: false,
            UseSystemCache: false,
            CachedContentName: null,
            SystemInstruction: FormatSystemInstruction,
            Contents: new[] { new GeminiTurn("user", request.Content) });

        var result = await gemini.RefineAsync(geminiRequest, cancellationToken);
        var content = StripCodeFences(result.Text);

        return new FormattedPromptMarkdownDto(content, result.PromptTokens, result.CandidateTokens);
    }

    private static string StripCodeFences(string raw)
    {
        var text = (raw ?? string.Empty).Replace("\r\n", "\n").Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstNewline = text.IndexOf('\n');
        if (firstNewline < 0)
        {
            return text.Trim('`').Trim();
        }

        var body = text[(firstNewline + 1)..];
        var closingFence = body.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFence >= 0)
        {
            body = body[..closingFence];
        }

        return body.Trim();
    }
}