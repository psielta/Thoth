using FluentAssertions;
using Thoth.Application.Common.Exceptions;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Commands.FormatPromptMarkdown;

namespace Thoth.Application.UnitTests;

public sealed class FormatPromptMarkdownHandlerTests
{
    private static FormatPromptMarkdownHandler CreateHandler(
        RecordingGeminiClient gemini,
        bool includeModel = true) =>
        new(gemini, new StubModelCatalog(includeModel));

    private static FormatPromptMarkdownCommand Command(string content = "Texto plano sem estrutura") =>
        new(
            content,
            StubModelCatalog.ModelId,
            0.2,
            new GeminiThinking("none", null, null));

    [Fact]
    public async Task Format_sends_format_instruction_and_user_content()
    {
        var gemini = new RecordingGeminiClient
        {
            ResponseText = "## Objetivo\n\n- Item um\n- Item dois",
        };
        var handler = CreateHandler(gemini);

        var result = await handler.Handle(Command("objetivo fazer x item um item dois"), CancellationToken.None);

        result.Content.Should().Be("## Objetivo\n\n- Item um\n- Item dois");
        result.PromptTokens.Should().Be(11);
        result.CandidateTokens.Should().Be(7);

        var instruction = gemini.LastRefineRequest!.SystemInstruction;
        instruction.Should().Contain("Markdown bem estruturado");
        instruction.Should().Contain("Preserve menções @caminho/arquivo intactas");
        instruction.Should().Contain("NÃO envolva todo o conteúdo em cercas de código");
        gemini.LastRefineRequest.Contents.Should()
            .ContainSingle(turn => turn.Role == "user" && turn.Text == "objetivo fazer x item um item dois");
    }

    [Fact]
    public async Task Format_strips_surrounding_code_fences()
    {
        var gemini = new RecordingGeminiClient
        {
            ResponseText = "```markdown\n## Titulo\n\nCorpo\n```",
        };
        var handler = CreateHandler(gemini);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.Content.Should().Be("## Titulo\n\nCorpo");
    }

    [Fact]
    public async Task Format_throws_when_model_not_found()
    {
        var gemini = new RecordingGeminiClient();
        var handler = CreateHandler(gemini, includeModel: false);

        var act = () => handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Modelo*");
    }
}