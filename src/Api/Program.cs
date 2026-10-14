using FastEndpoints;
using FastEndpoints.Swagger;
using Spine.Api.Cli;
using Spine.Api.Errors;
using Spine.Api.Health;
using Spine.Infrastructure;
using Spine.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

if (args.Length > 0 && args[0] == "import-content")
{
    return await ImportContentCommand.RunAsync(args[1..]);
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext();
    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console();
    }
    else
    {
        configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
    }
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReviewExceptionHandler>();
builder.Services.AddFastEndpoints();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);
if (builder.Environment.IsDevelopment())
{
    builder.Services.SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Spine API";
            s.Version = "v1";
        };
    });
}

await using var app = builder.Build();

var startupTimeout = TimeSpan.FromSeconds(app.Configuration.GetValue("DB_STARTUP_TIMEOUT_SECONDS", 90));
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        var stopping = app.Lifetime.ApplicationStopping;
        await scope.ServiceProvider.GetRequiredService<StartupMigrator>().MigrateAsync(startupTimeout, stopping);
        await scope.ServiceProvider.GetRequiredService<LearnerSeeder>().SeedAsync(stopping);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database start-up failed; refusing to serve");
        return 1;
    }
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapHealth();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    c.Errors.UseProblemDetails();
});
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

await app.RunAsync();
return 0;

public partial class Program;
