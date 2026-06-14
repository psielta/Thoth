namespace Thoth.Application.Common.Interfaces;

public interface IWorkspaceEditorLauncher
{
    Task OpenVsCodeAsync(string absolutePath, CancellationToken cancellationToken);
}
