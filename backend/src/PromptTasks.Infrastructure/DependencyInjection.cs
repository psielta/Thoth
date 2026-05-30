using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.Configure<LinkedDocumentOptions>(options =>
        {
            var section = configuration.GetSection("LinkedDocuments");
            if (long.TryParse(section["MaxFileSizeBytes"], out var maxFileSizeBytes))
            {
                options.MaxFileSizeBytes = maxFileSizeBytes;
            }

            if (int.TryParse(section["DebounceMilliseconds"], out var debounceMilliseconds))
            {
                options.DebounceMilliseconds = debounceMilliseconds;
            }

            if (int.TryParse(section["ReconcileSeconds"], out var reconcileSeconds))
            {
                options.ReconcileSeconds = reconcileSeconds;
            }

            if (bool.TryParse(section["AllowUncPaths"], out var allowUncPaths))
            {
                options.AllowUncPaths = allowUncPaths;
            }
        });
        services.AddScoped<ILinkedDocumentFileService, LinkedDocumentFileService>();
        services.AddScoped<ILinkedDocumentSyncService, LinkedDocumentSyncService>();
        services.AddSingleton<LinkedDocumentWatcherService>();
        services.AddSingleton<ILinkedDocumentWatchCoordinator>(provider =>
            provider.GetRequiredService<LinkedDocumentWatcherService>());
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<LinkedDocumentWatcherService>());

        return services;
    }
}
