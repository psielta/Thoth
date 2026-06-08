using Microsoft.EntityFrameworkCore;
using PromptTasks.Api;
using PromptTasks.Api.Hubs;
using PromptTasks.Application;
using PromptTasks.Infrastructure;
using PromptTasks.Infrastructure.Persistence;
using Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Host.UseWindowsService();

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5191");
}

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors("spa");
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PromptHub>("/hubs/prompts");

app.Map("/api/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));
app.Map("/hubs/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.Run();

public partial class Program;
