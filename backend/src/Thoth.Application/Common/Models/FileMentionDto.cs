namespace Thoth.Application.Common.Models;

public sealed record FileMentionDto(string Id, string? Label)
{
    public string RelativePath => Id;
}
