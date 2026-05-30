using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PromptTasks.Infrastructure.Services;

namespace PromptTasks.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=prompttasks;Username=prompttasks;Password=prompttasks";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options, new SystemCurrentUser(), new DateTimeProvider());
    }
}
