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
        "Você é um formatador de Markdown, NÃO um editor de conteúdo nem um otimizador de prompts. " +
        "Sua única tarefa é aplicar sintaxe Markdown ao texto do usuário para melhorar a legibilidade estrutural. " +
        "\n\nPROIBIDO:\n" +
        "- Reescrever, parafrasear, corrigir gramática ou melhorar clareza\n" +
        "- Resumir, expandir, omitir ou substituir trechos\n" +
        "- Reorganizar ideias ou mudar a ordem lógica do conteúdo\n" +
        "- Adicionar títulos, seções, listas ou explicações que não correspondam ao texto original\n" +
        "- Inventar ou inferir conteúdo que não esteja no texto original\n" +
        "- Otimizar engenharia de prompt (isso é outra ferramenta)\n" +
        "\nPERMITIDO:\n" +
        "- Inserir ##/### quando o texto já indica seções\n" +
        "- Converter enumerações implícitas em listas (- ou 1.)\n" +
        "- Aplicar **negrito** ou *itálico* apenas se já houver ênfase óbvia no original\n" +
        "- Usar `code` ou ```lang``` para trechos que já são código, comandos, paths ou hashes\n" +
        "- Inserir linhas em branco para separar blocos já distintos\n" +
        "- Preservar menções @caminho/arquivo exatamente como estão\n" +
        "\nREGRAS:\n" +
        "- Mantenha as mesmas palavras, frases e ordem do original sempre que possível\n" +
        "- Se o texto já estiver em Markdown aceitável, devolva-o com ajustes mínimos de espaçamento\n" +
        "- Responda APENAS com o Markdown formatado\n" +
        "- NÃO envolva todo o conteúdo em cercas de código\n" +
        "- Não adicione comentários ou explicações fora do conteúdo";

    private const string UserPromptPrefix =
        "Formate APENAS a estrutura Markdown do texto abaixo. " +
        "Não altere palavras, não reescreva e não melhore o conteúdo.\n\n";

    public async Task<FormattedPromptMarkdownDto> Handle(
        FormatPromptMarkdownCommand request,
        CancellationToken cancellationToken)
    {
        if (catalog.GetModel(request.Model) is null)
            throw new NotFoundException($"Modelo '{request.Model}' não encontrado.");

        var temperature = Math.Min(request.Temperature, 0.1);
        var userPrompt = UserPromptPrefix + request.Content;

        var geminiRequest = new GeminiGenerationRequest(
            Model: request.Model,
            Temperature: temperature,
            Thinking: new GeminiThinking("none", null, null),
            IncludeThoughts: false,
            UseSystemCache: false,
            CachedContentName: null,
            SystemInstruction: FormatSystemInstruction,
            Contents: new[] { new GeminiTurn("user", userPrompt) });

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