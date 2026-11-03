using FastEndpoints;
using Spine.Api.Cli;
using Spine.Api.Errors;
using Spine.Api.Health;
using Spine.Api.Startup;
using Spine.Infrastructure;
using Spine.Infrastructure.Persistence;
using Serilog;

if (args.Length > 0 && args[0] == "import-content")
{
    return await ImportContentCommand.RunAsync(args[1..]);
}

var builder = WebApplication.CreateBuilder(args);
builder.AddSpineLogging();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ReviewExceptionHandler>();
builder.Services.AddFastEndpoints();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);
builder.Services.AddSpineSwagger(builder.Environment);

await using var app = builder.Build();
if (!await app.InitializeDatabaseAsync())
{
    return 1;
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapHealth();
app.UseSpineEndpoints();
app.UseSpineSwagger();

await app.RunAsync();
return 0;

public partial class Program;
