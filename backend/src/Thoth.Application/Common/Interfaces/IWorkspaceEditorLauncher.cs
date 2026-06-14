namespace Thoth.Application.Common.Interfaces;

public interface IWorkspaceEditorLauncher
{
    Task OpenVsCodeAsync(string absolutePath, CancellationToken cancellationToken);

    Task OpenFileInVsCodeAsync(string workspaceAbsolutePath, string fileAbsolutePath, CancellationToken cancellationToken);
}
