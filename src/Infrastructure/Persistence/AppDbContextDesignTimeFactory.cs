using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InterviewPrep.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the context without starting the web host. Reads
/// <c>ConnectionStrings__Default</c>; the password may come from <c>PGPASSWORD</c> (honoured by Npgsql).
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string FallbackConnectionString = "Host=localhost;Port=5432;Database=interviewprep;Username=interviewprep";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? FallbackConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(options, connectionString);
        return new AppDbContext(options.Options);
    }
}
