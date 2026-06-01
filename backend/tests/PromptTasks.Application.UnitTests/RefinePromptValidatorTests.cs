using FluentAssertions;
using PromptTasks.Application.Common.Models;
using PromptTasks.Application.Features.Ai.Commands.RefinePrompt;

namespace PromptTasks.Application.UnitTests;

public sealed class RefinePromptValidatorTests
{
    private readonly RefinePromptValidator _validator = new();

    [Fact]
    public void Allows_empty_context_files_and_valid_relative_paths()
    {
        var emptyResult = _validator.Validate(CreateCommand(contextFiles: Array.Empty<string>()));
        var validResult = _validator.Validate(CreateCommand(
            workingDirectoryId: Guid.CreateVersion7(),
            contextFiles: new[] { "src/main.cs", "docs/readme.md" }));

        emptyResult.IsValid.Should().BeTrue();
        validResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_null_context_files_without_throwing()
    {
        var command = new RefinePromptCommand(
            "Improve this",
            "gemini-test",
            0.4,
            new GeminiThinking("none", 0, null),
            null,
            null!,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RefinePromptCommand.ContextFiles));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("src/../secret.txt")]
    [InlineData("C:/secret.txt")]
    [InlineData("")]
    public void Rejects_invalid_context_file_paths(string path)
    {
        var result = _validator.Validate(CreateCommand(
            workingDirectoryId: Guid.CreateVersion7(),
            contextFiles: new[] { path }));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_more_than_twenty_context_files()
    {
        var files = Enumerable.Range(0, 21)
            .Select(index => $"src/file-{index}.cs")
            .ToArray();

        var result = _validator.Validate(CreateCommand(
            workingDirectoryId: Guid.CreateVersion7(),
            contextFiles: files));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_context_files_without_working_directory()
    {
        var result = _validator.Validate(CreateCommand(contextFiles: new[] { "src/main.cs" }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RefinePromptCommand.WorkingDirectoryId));
    }

    [Fact]
    public void Rejects_custom_instructions_over_limit()
    {
        var result = _validator.Validate(CreateCommand(
            contextFiles: Array.Empty<string>(),
            customInstructions: new string('a', 20_001)));

        result.IsValid.Should().BeFalse();
    }

    private static RefinePromptCommand CreateCommand(
        Guid? workingDirectoryId = null,
        IReadOnlyList<string>? contextFiles = null,
        string? customInstructions = null) =>
        new(
            "Improve this",
            "gemini-test",
            0.4,
            new GeminiThinking("none", 0, null),
            workingDirectoryId,
            contextFiles ?? Array.Empty<string>(),
            customInstructions);
}
