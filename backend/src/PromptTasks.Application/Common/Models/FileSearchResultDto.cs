namespace PromptTasks.Application.Common.Models;

public sealed record FileSearchResultDto(
    string RelativePath,
    string FileName,
    bool IsDirectory,
    int Score);
