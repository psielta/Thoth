using FluentAssertions;
using Thoth.Application.Common.Models;
using Thoth.Application.Features.Ai.Commands.FormatPromptMarkdown;

namespace Thoth.Application.UnitTests;

public sealed class FormatPromptMarkdownValidatorTests
{
    private readonly FormatPromptMarkdownValidator _validator = new();

    [Fact]
    public void Allows_valid_command()
    {
        var result = _validator.Validate(CreateCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_content(string content)
    {
        var result = _validator.Validate(CreateCommand(content: content));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(FormatPromptMarkdownCommand.Content));
    }

    [Fact]
    public void Rejects_content_over_limit()
    {
        var result = _validator.Validate(CreateCommand(content: new string('a', 200_001)));

        result.IsValid.Should().BeFalse();
    }

    private static FormatPromptMarkdownCommand CreateCommand(string content = "Texto do prompt") =>
        new(
            content,
            "gemini-test",
            0.2,
            new GeminiThinking("none", null, null));
}