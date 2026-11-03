using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Persistence.Startup;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LearnerSeederTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Seeding_twice_leaves_exactly_one_local_learner()
    {
        await using (var db = fixture.CreateContext())
        {
            await new LearnerSeeder(db).SeedAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            await new LearnerSeeder(db).SeedAsync();
        }

        await using var verify = fixture.CreateContext();
        var users = await verify.Users.AsNoTracking().ToListAsync();
        users.Count.ShouldBe(1);
        users[0].Id.ShouldBe(LocalLearner.Id);
        users[0].DisplayName.ShouldBe(LocalLearner.DisplayName);

        var language = await verify.Database.SqlQueryRaw<string>("SELECT preferred_language AS \"Value\" FROM users").SingleAsync();
        language.ShouldBe("en");
    }
}
