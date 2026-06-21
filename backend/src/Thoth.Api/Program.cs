using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Thoth.Api;
using Thoth.Api.Hubs;
using Thoth.Application;
using Thoth.Infrastructure;
using Thoth.Infrastructure.Persistence;
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

static void PreventSpaShellCaching(StaticFileResponseContext context)
{
    if (!string.Equals(context.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var headers = context.Context.Response.Headers;
    headers.CacheControl = "no-store, no-cache, must-revalidate";
    headers.Pragma = "no-cache";
    headers.Expires = "0";
}

var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = PreventSpaShellCaching
};

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors("spa");
app.UseDefaultFiles();
app.UseStaticFiles(staticFileOptions);

app.MapControllers();
app.MapHub<PromptHub>("/hubs/prompts");

app.Map("/api/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));
app.Map("/hubs/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"));
app.MapFallbackToFile("index.html", staticFileOptions);

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
