using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Thoth.Infrastructure.Services;

namespace Thoth.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5459;Database=prompttasks;Username=prompttasks;Password=prompttasks";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options, new SystemCurrentUser(), new DateTimeProvider());
    }
}
