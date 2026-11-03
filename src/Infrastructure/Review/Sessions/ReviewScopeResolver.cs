using Spine.Application.Knowledge;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>Validates a requested scope against live content; deprecated or unknown → <see cref="ReviewError.ScopeNotFound"/>.</summary>
public sealed class ReviewScopeResolver(AppDbContext db)
{
    public async Task<ScopeDto> ResolveAsync(ReviewScope scope, Language language, CancellationToken ct)
    {
        scope.Validate();
        switch (scope.Type)
        {
            case SessionScopeType.All:
                return new ScopeDto("all", null, null, null, null);
            case SessionScopeType.Domain:
                {
                    var row = await db.Domains.AsNoTracking()
                        .Where(d => d.Slug == scope.Domain && d.DeprecatedAt == null)
                        .Select(d => new { d.Title, d.TitleUk })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No domain '{scope.Domain}'.");
                    return new ScopeDto("domain", scope.Domain, null, LocalizedTitle.Of(language, row.Title, row.TitleUk), null);
                }

            case SessionScopeType.Topic:
                {
                    var row = await db.Topics.AsNoTracking()
                        .Where(t => t.Slug == scope.Topic && t.Domain!.Slug == scope.Domain && t.DeprecatedAt == null && t.Domain.DeprecatedAt == null)
                        .Select(t => new { DomainTitle = t.Domain!.Title, DomainTitleUk = t.Domain.TitleUk, t.Title, t.TitleUk })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No topic '{scope.Domain}/{scope.Topic}'.");
                    return new ScopeDto(
                        "topic",
                        scope.Domain,
                        scope.Topic,
                        LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk),
                        LocalizedTitle.Of(language, row.Title, row.TitleUk));
                }

            case SessionScopeType.Concept:
                {
                    var row = await db.Concepts.AsNoTracking()
                        .Where(c => c.Slug == scope.Concept && c.Topic!.Slug == scope.Topic && c.Topic.Domain!.Slug == scope.Domain)
                        .Where(c => c.DeprecatedAt == null && c.Topic!.DeprecatedAt == null && c.Topic.Domain!.DeprecatedAt == null)
                        .Select(c => new
                        {
                            DomainTitle = c.Topic!.Domain!.Title,
                            DomainTitleUk = c.Topic.Domain.TitleUk,
                            TopicTitle = c.Topic.Title,
                            TopicTitleUk = c.Topic.TitleUk,
                            Requested = c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                            En = c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                        })
                        .SingleOrDefaultAsync(ct)
                        ?? throw new ReviewException(ReviewError.ScopeNotFound, $"No concept '{scope.Domain}/{scope.Topic}/{scope.Concept}'.");
                    return new ScopeDto(
                        "concept",
                        scope.Domain,
                        scope.Topic,
                        LocalizedTitle.Of(language, row.DomainTitle, row.DomainTitleUk),
                        LocalizedTitle.Of(language, row.TopicTitle, row.TopicTitleUk),
                        scope.Concept,
                        row.Requested ?? row.En ?? scope.Concept);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope.Type, null);
        }
    }
}
