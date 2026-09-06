using FastEndpoints;
using FastEndpoints.Swagger;
using InterviewPrep.Api.Cli;
using InterviewPrep.Api.Health;
using InterviewPrep.Infrastructure;
using InterviewPrep.Infrastructure.Persistence;
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
builder.Services.AddFastEndpoints();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);
if (builder.Environment.IsDevelopment())
{
    builder.Services.SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "InterviewPrep API";
            s.Version = "v1";
        };
    });
}

var app = builder.Build();

var startupTimeout = TimeSpan.FromSeconds(app.Configuration.GetValue("DB_STARTUP_TIMEOUT_SECONDS", 90));
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<StartupMigrator>().MigrateAsync(startupTimeout);
        await scope.ServiceProvider.GetRequiredService<LearnerSeeder>().SeedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database start-up failed; refusing to serve");
        return 1;
    }
}

app.UseSerilogRequestLogging();
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
