using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Spine.Infrastructure.Persistence;

namespace Spine.Api.Tests.Snapshots;

/// <summary>
/// An isolated API + database for the response snapshots (feature 008): a pinned clock so every timestamp is
/// deterministic, and a command-counting interceptor so each case can assert its SQL round trips.
/// </summary>
public sealed class SnapshotFixture : ApiFixture
{
    public static readonly DateTimeOffset Start = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; } = new(Start);

    public CommandCountingInterceptor Commands { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock));
        services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(Commands));
    }
}

[CollectionDefinition("snapshots")]
public sealed class SnapshotCollection : ICollectionFixture<SnapshotFixture>;
