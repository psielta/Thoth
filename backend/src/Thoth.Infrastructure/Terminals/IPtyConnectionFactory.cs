namespace Thoth.Infrastructure.Terminals;

public interface IPtyConnectionFactory
{
    Task<IPtyConnection> CreateAsync(
        string shell,
        string cwd,
        int cols,
        int rows,
        CancellationToken cancellationToken);
}