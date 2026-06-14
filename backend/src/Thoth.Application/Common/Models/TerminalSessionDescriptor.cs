namespace Thoth.Application.Common.Models;

public sealed record TerminalSessionDescriptor(
    Guid Id,
    Guid? PromptId,
    string Shell,
    string Cwd,
    DateTimeOffset CreatedAtUtc,
    // Relativo a visao: true quando o terminal pertence a um prompt filho do prompt/grupo exibido.
    bool IsChild = false,
    // Titulo do prompt dono (o filho) quando IsChild; usado para o badge na UI.
    string? OwnerPromptTitle = null);