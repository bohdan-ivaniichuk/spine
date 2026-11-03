using Spine.Application.Knowledge;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>Titles for a stored session's scope; lenient, because content may have been deprecated since.</summary>
public sealed class SessionScopeDescriber(AppDbContext db)
{
    public async Task<ScopeDto> DescribeAsync(Session session, Language language, CancellationToken ct)
    {
        var type = session.ScopeType.ToDbValue();
        if (session.ScopeDomainSlug is null)
        {
            return new ScopeDto(type, null, null, null, null);
        }

        var domain = await db.Domains.AsNoTracking()
            .Where(d => d.Slug == session.ScopeDomainSlug)
            .Select(d => new { d.Title, d.TitleUk })
            .SingleOrDefaultAsync(ct);
        var domainTitle = domain is null ? null : LocalizedTitle.Of(language, domain.Title, domain.TitleUk);
        string? topicTitle = null;
        if (session.ScopeTopicSlug is not null)
        {
            var topic = await db.Topics.AsNoTracking()
                .Where(t => t.Slug == session.ScopeTopicSlug && t.Domain!.Slug == session.ScopeDomainSlug)
                .Select(t => new { t.Title, t.TitleUk })
                .SingleOrDefaultAsync(ct);
            topicTitle = topic is null ? null : LocalizedTitle.Of(language, topic.Title, topic.TitleUk);
        }

        string? conceptTitle = null;
        if (session.ScopeConceptSlug is not null)
        {
            var concept = await db.Concepts.AsNoTracking()
                .Where(c => c.Slug == session.ScopeConceptSlug && c.Topic!.Slug == session.ScopeTopicSlug && c.Topic.Domain!.Slug == session.ScopeDomainSlug)
                .Select(c => new
                {
                    Requested = c.Translations.Where(t => t.Language == language).Select(t => t.Title).FirstOrDefault(),
                    En = c.Translations.Where(t => t.Language == Language.En).Select(t => t.Title).FirstOrDefault(),
                })
                .SingleOrDefaultAsync(ct);
            conceptTitle = concept is null ? null : concept.Requested ?? concept.En ?? session.ScopeConceptSlug;
        }

        return new ScopeDto(type, session.ScopeDomainSlug, session.ScopeTopicSlug, domainTitle, topicTitle, session.ScopeConceptSlug, conceptTitle);
    }

    /// <summary>The review scope a stored session was started with.</summary>
    public static ReviewScope ScopeOf(Session session) =>
        new(session.ScopeType, session.ScopeDomainSlug, session.ScopeTopicSlug, session.ScopeConceptSlug);
}
