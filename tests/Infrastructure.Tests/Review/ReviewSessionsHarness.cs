using Microsoft.Extensions.Options;
using Spine.Application.Review;
using Spine.Application.Review.AnswerQuestion;
using Spine.Application.Review.FinishSession;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Application.Review.GetReviewOverview;
using Spine.Application.Review.Sessions;
using Spine.Application.Review.Shared;
using Spine.Domain.Content;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.AnswerQuestion;
using Spine.Infrastructure.Review.FinishSession;
using Spine.Infrastructure.Review.GetCurrentSession;
using Spine.Infrastructure.Review.GetReviewCoverage;
using Spine.Infrastructure.Review.GetReviewOverview;
using Spine.Infrastructure.Review.GetSession;
using Spine.Infrastructure.Review.Sessions;
using Spine.Infrastructure.Review.StartSession;
using Spine.Infrastructure.Review.StartWeakSession;

namespace Spine.Infrastructure.Tests.Review;

/// <summary>
/// The review use cases behind the method names the review test suites were written against (feature 008 split the
/// single review service into one class per use case); built on one context, clock and options like a request scope.
/// </summary>
public sealed class ReviewSessionsHarness
{
    private readonly GetReviewOverviewUseCase _overview;
    private readonly GetReviewCoverageUseCase _coverage;
    private readonly StartSessionUseCase _start;
    private readonly StartWeakSessionUseCase _startWeak;
    private readonly GetCurrentSessionUseCase _current;
    private readonly GetSessionUseCase _get;
    private readonly AnswerQuestionUseCase _answer;
    private readonly FinishSessionUseCase _finish;

    public ReviewSessionsHarness(AppDbContext db, ISchedulingAlgorithm algorithm, TimeProvider clock, IOptions<ReviewOptions> options)
    {
        var resolver = new ReviewScopeResolver(db);
        var describer = new SessionScopeDescriber(db);
        var projector = new SessionProjector(db, describer);
        var starter = new SessionStarter(db, clock, resolver, projector);
        _overview = new GetReviewOverviewUseCase(db, resolver, clock, options);
        _coverage = new GetReviewCoverageUseCase(db, clock);
        _start = new StartSessionUseCase(db, starter);
        _startWeak = new StartWeakSessionUseCase(db, starter, options);
        _current = new GetCurrentSessionUseCase(db, projector);
        _get = new GetSessionUseCase(projector);
        _answer = new AnswerQuestionUseCase(db, algorithm, clock);
        _finish = new FinishSessionUseCase(db, clock, describer);
    }

    public Task<ReviewOverviewDto> GetOverviewAsync(ReviewScope scope, Language language = Language.En, CancellationToken ct = default) =>
        _overview.ExecuteAsync(scope, language, ct);

    public Task<SessionDto> StartAsync(ReviewScope scope, int totalLimit, int newLimit, Language language = Language.En, CancellationToken ct = default) =>
        _start.ExecuteAsync(scope, totalLimit, newLimit, language, ct);

    public Task<SessionDto> StartWeakAsync(ReviewScope scope, int totalLimit, Language language = Language.En, CancellationToken ct = default) =>
        _startWeak.ExecuteAsync(scope, totalLimit, language, ct);

    public Task<SessionDto?> GetCurrentAsync(Language language = Language.En, CancellationToken ct = default) =>
        _current.ExecuteAsync(language, ct);

    public Task<SessionDto?> GetAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default) =>
        _get.ExecuteAsync(sessionId, language, ct);

    public Task<AnswerFeedbackDto> AnswerAsync(Guid sessionId, string questionId, int selectedIndex, Language language = Language.En, CancellationToken ct = default) =>
        _answer.ExecuteAsync(sessionId, questionId, selectedIndex, language, ct);

    public Task<SessionSummaryDto> FinishAsync(Guid sessionId, Language language = Language.En, CancellationToken ct = default) =>
        _finish.ExecuteAsync(sessionId, language, ct);

    public Task<ReviewCoverageDto> GetCoverageAsync(CancellationToken ct = default) => _coverage.ExecuteAsync(ct);
}
