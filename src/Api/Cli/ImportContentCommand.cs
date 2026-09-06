using InterviewPrep.Application.Content;
using InterviewPrep.Infrastructure;
using Serilog;
using Serilog.Events;

namespace InterviewPrep.Api.Cli;

/// <summary>
/// `import-content [--content-dir &lt;path&gt;] [--validate-only] [--dry-run]` — see
/// specs/001-foundation/contracts/import-cli.md. Builds a plain host (no Kestrel) with the same configuration
/// surface as the web host and returns the documented exit code.
/// </summary>
public static class ImportContentCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? contentDir = null;
        var validateOnly = false;
        var dryRun = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--content-dir" when i + 1 < args.Length:
                    contentDir = args[++i];
                    break;
                case "--validate-only":
                    validateOnly = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "-h" or "--help":
                    Console.WriteLine("usage: import-content [--content-dir <path>] [--validate-only] [--dry-run]");
                    return 0;
                default:
                    Console.Error.WriteLine($"unknown argument '{args[i]}' (usage: import-content [--content-dir <path>] [--validate-only] [--dry-run])");
                    return (int)ImportExitCode.ValidationFailed;
            }
        }

        contentDir ??= Environment.GetEnvironmentVariable("CONTENT_DIR") ?? FindContentDir();
        contentDir = Path.GetFullPath(contentDir);

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [] });
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(new LoggerConfiguration()
            .MinimumLevel.Warning()
            .MinimumLevel.Override("InterviewPrep", LogEventLevel.Information)
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger(), dispose: true);
        builder.Services.AddInfrastructure(builder.Configuration);
        using var host = builder.Build();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            using var scope = host.Services.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<IContentImporter>();
            var result = await importer.ImportAsync(new ImportOptions(contentDir, validateOnly, dryRun), cts.Token);
            return Report(result, contentDir);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            Console.Error.WriteLine("import cancelled; nothing written");
            return (int)ImportExitCode.Failure;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"import failed: {ex.GetBaseException().Message}");
            host.Services.GetRequiredService<ILogger<IContentImporter>>().LogError(ex, "import failed");
            return (int)ImportExitCode.Failure;
        }
    }

    /// <summary>`./content` from the working directory, else the nearest ancestor holding content/schema (dev inner loop).</summary>
    private static string FindContentDir()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "schema", "content.schema.json")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "content");
    }

    private static int Report(ImportResult result, string contentDir)
    {
        foreach (var error in result.Errors)
        {
            Console.Error.WriteLine(error.ToString());
        }

        if (result.Errors.Count > 0)
        {
            Console.Error.WriteLine($"{result.Errors.Count} error(s); nothing written.");
        }

        if (result.Summary is not null)
        {
            Console.WriteLine($"content: {contentDir}");
            Console.Write(result.Summary.Render());
        }

        if (result.Message is not null)
        {
            Console.WriteLine(result.Message);
        }

        return (int)result.ExitCode;
    }
}
