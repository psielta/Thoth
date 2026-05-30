using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PromptTasks.Application.Common.Interfaces;
using PromptTasks.Infrastructure.FileSystem;
using PromptTasks.Infrastructure.Persistence;
using PromptTasks.Infrastructure.Services;

namespace PromptTasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=prompttasks;Username=prompttasks;Password=prompttasks";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddMemoryCache();
        services.AddScoped<IWorkspaceFileService, WorkspaceFileService>();

        return services;
    }
}
