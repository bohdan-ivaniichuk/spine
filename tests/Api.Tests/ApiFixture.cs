using System.Text.Json;
using Spine.Application.Content;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Spine.Api.Tests;

/// <summary>
/// One PostgreSQL container + one in-process API per collection. The API boots exactly as in production
/// (migrations, learner seed), then tests call <see cref="ImportAsync"/> to load the frozen fixture content set
/// (<c>tests/Fixtures/api-content</c>) validated against the real repo schemas (<c>content/schema</c>), so API tests do not
/// change when the repo content grows while schema drift is still caught. The repo content itself is guarded by
/// <c>RepoContentValidatesTests</c> in Application.Tests.
/// </summary>
public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly SemaphoreSlim _importGate = new(1, 1);
    private WebApplicationFactory<Program>? _factory;
    private bool _imported;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory!.Services;

    /// <summary>Repo root, found by walking up from the test binaries until the content schema is visible.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>
    /// The frozen fixture content: <c>manifest.json</c> and <c>dotnet/{clr-memory,async}.json</c> (see <c>ReviewApi</c> for
    /// the shape the tests rely on). It has no <c>schema/</c>; <see cref="ImportAsync"/> adds the repo schemas.
    /// </summary>
    public static string ContentDir => Path.Combine(RepoRoot, "tests", "Fixtures", "api-content");

    public static string SchemaDir => Path.Combine(RepoRoot, "content", "schema");

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
            b.UseSetting("DB_STARTUP_TIMEOUT_SECONDS", "30");
        });
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _db.DisposeAsync();
    }

    /// <summary>Imports the fixture content once; later calls are no-ops so every test class sees the same data.</summary>
    public async Task ImportAsync()
    {
        await _importGate.WaitAsync();
        try
        {
            if (_imported)
            {
                return;
            }

            var contentDir = AssembleContentDir();
            try
            {
                using var scope = Services.CreateScope();
                var importer = scope.ServiceProvider.GetRequiredService<IContentImporter>();
                var result = await importer.ImportAsync(new ImportOptions(contentDir));
                result.ExitCode.ShouldBe(ImportExitCode.Success, string.Join("\n", result.Errors.Select(e => e.ToString())));
                _imported = true;
            }
            finally
            {
                Directory.Delete(contentDir, recursive: true);
            }
        }
        finally
        {
            _importGate.Release();
        }
    }

    /// <summary>A throw-away content directory: the fixture files plus <c>schema/</c> copied from the repo.</summary>
    private static string AssembleContentDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "spine-api-tests", Guid.NewGuid().ToString("N"));
        foreach (var file in Directory.GetFiles(ContentDir, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(dir, Path.GetRelativePath(ContentDir, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        var schemaDir = Path.Combine(dir, "schema");
        Directory.CreateDirectory(schemaDir);
        foreach (var file in Directory.GetFiles(SchemaDir, "*.json"))
        {
            File.Copy(file, Path.Combine(schemaDir, Path.GetFileName(file)));
        }

        return dir;
    }

    /// <summary>Empties the progress tables so every review test class starts with no sessions, attempts or review states.</summary>
    public Task ResetProgressAsync() => ExecuteSqlAsync("TRUNCATE attempts, review_states, sessions");

    public async Task<int> ExecuteSqlAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_db.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ExecuteScalarAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_db.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        var value = await command.ExecuteScalarAsync();
        return value is DBNull ? null : value;
    }

    public async Task SetConceptDeprecatedAsync(string conceptSlug, DateTimeOffset? deprecatedAt)
    {
        var affected = await ExecuteSqlAsync(
            "UPDATE concepts SET deprecated_at = @at WHERE slug = @slug",
            new NpgsqlParameter("at", (object?)deprecatedAt ?? DBNull.Value),
            new NpgsqlParameter("slug", conceptSlug));
        affected.ShouldBe(1, $"concept '{conceptSlug}' should exist");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "content", "schema", "content.schema.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root (content/schema/content.schema.json) above " + AppContext.BaseDirectory);
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;

/// <summary>A second, isolated API + database used only for "empty database" assertions.</summary>
public sealed class EmptyApiFixture : ApiFixture;
