using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using PromptTasks.Api.ExceptionHandling;
using PromptTasks.Api.Realtime;
using PromptTasks.Application.Common.Interfaces;

namespace PromptTasks.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

        services.AddOpenApi();
        services.AddSignalR();
        services.AddScoped<IPromptNotifier, SignalRPromptNotifier>();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<PathTraversalExceptionHandler>();
        services.AddExceptionHandler<ConflictExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddPolicy("spa", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressMapClientErrors = false;
        });

        return services;
    }
}
