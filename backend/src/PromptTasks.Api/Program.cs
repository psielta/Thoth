using Microsoft.EntityFrameworkCore;
using PromptTasks.Api;
using PromptTasks.Api.Hubs;
using PromptTasks.Application;
using PromptTasks.Infrastructure;
using PromptTasks.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5080");

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("spa");

app.MapControllers();
app.MapHub<PromptHub>("/hubs/prompts");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);

    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.Run();

public partial class Program;
