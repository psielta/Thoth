using Thoth.Domain.Common;
using Thoth.Domain.Prompts;
using Thoth.Domain.Users;

namespace Thoth.Domain.WorkingDirectories;

public sealed class WorkingDirectory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string AbsolutePath { get; set; } = string.Empty;
    public bool RespectGitignore { get; set; } = true;
    public bool EnableAiContext { get; set; }
    public string? TaskNumberPattern { get; set; }

    public User? Owner { get; set; }
    public ICollection<Prompt> Prompts { get; } = new List<Prompt>();
}
