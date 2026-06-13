using Porta.Pty;

namespace Thoth.Infrastructure.Terminals;

public sealed class PortaPtyConnectionFactory : IPtyConnectionFactory
{
    public async Task<IPtyConnection> CreateAsync(
        string shell,
        string cwd,
        int cols,
        int rows,
        CancellationToken cancellationToken)
    {
        var options = new PtyOptions
        {
            Name = "ThothTerminal",
            Cols = cols,
            Rows = rows,
            Cwd = cwd,
            App = shell
        };

        var connection = await PtyProvider.SpawnAsync(options, cancellationToken);
        return new PortaPtyConnectionAdapter(connection);
    }
}