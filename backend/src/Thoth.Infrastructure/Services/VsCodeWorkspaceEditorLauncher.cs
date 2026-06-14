using System.ComponentModel;
using System.Diagnostics;
using Thoth.Application.Common.Interfaces;

namespace Thoth.Infrastructure.Services;

public sealed class VsCodeWorkspaceEditorLauncher : IWorkspaceEditorLauncher
{
    public Task OpenVsCodeAsync(string absolutePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(absolutePath))
        {
            throw new InvalidOperationException("O diretório do workspace não existe mais no disco.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = QuoteArgument(absolutePath),
                WorkingDirectory = absolutePath,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Não foi possível abrir o VS Code.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                "Não foi possível encontrar o comando 'code'. Instale o VS Code CLI no PATH para abrir o workspace.",
                exception);
        }

        return Task.CompletedTask;
    }

    public Task OpenFileInVsCodeAsync(string workspaceAbsolutePath, string fileAbsolutePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(workspaceAbsolutePath))
        {
            throw new InvalidOperationException("O diretório do workspace não existe mais no disco.");
        }

        if (!File.Exists(fileAbsolutePath))
        {
            throw new InvalidOperationException("O arquivo não existe mais no disco.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"{QuoteArgument(workspaceAbsolutePath)} --goto {QuoteArgument(fileAbsolutePath)}",
                WorkingDirectory = workspaceAbsolutePath,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException("Não foi possível abrir o VS Code.");
            }
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                "Não foi possível encontrar o comando 'code'. Instale o VS Code CLI no PATH para abrir o arquivo.",
                exception);
        }

        return Task.CompletedTask;
    }

    private static string QuoteArgument(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
