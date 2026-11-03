using Spine.Application.Review.Activity;
using Spine.Application.Review.AnswerQuestion;
using Spine.Application.Review.FinishSession;
using Spine.Application.Review.GetCurrentSession;
using Spine.Application.Review.GetReviewCoverage;
using Spine.Application.Review.GetReviewOverview;
using Spine.Application.Review.GetSession;
using Spine.Application.Review.Readiness;
using Spine.Application.Review.Shared;
using Spine.Application.Review.StartSession;
using Spine.Application.Review.StartWeakSession;
using Spine.Application.Review.WeakSpots;
using Spine.Application.Review;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Review.Activity;
using Spine.Infrastructure.Review.AnswerQuestion;
using Spine.Infrastructure.Review.FinishSession;
using Spine.Infrastructure.Review.GetCurrentSession;
using Spine.Infrastructure.Review.GetReviewCoverage;
using Spine.Infrastructure.Review.GetReviewOverview;
using Spine.Infrastructure.Review.GetSession;
using Spine.Infrastructure.Review.Readiness;
using Spine.Infrastructure.Review.Sessions;
using Spine.Infrastructure.Review.StartSession;
using Spine.Infrastructure.Review.StartWeakSession;
using Spine.Infrastructure.Review.WeakSpots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Spine.Infrastructure.Review;

/// <summary>
/// The review module: one scoped class per use case, the shared session collaborators, the read queries, the
/// scheduling algorithm and the session-size options. Needs an <c>AppDbContext</c> and a <see cref="TimeProvider"/>.
/// </summary>
public static class ReviewServiceCollectionExtensions
{
    public static IServiceCollection AddReview(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISchedulingAlgorithm, Sm2Algorithm>();

        services.AddScoped<ReviewScopeResolver>();
        services.AddScoped<SessionScopeDescriber>();
        services.AddScoped<SessionProjector>();
        services.AddScoped<SessionStarter>();

        services.AddScoped<IGetReviewOverviewUseCase, GetReviewOverviewUseCase>();
        services.AddScoped<IGetReviewCoverageUseCase, GetReviewCoverageUseCase>();
        services.AddScoped<IStartSessionUseCase, StartSessionUseCase>();
        services.AddScoped<IStartWeakSessionUseCase, StartWeakSessionUseCase>();
        services.AddScoped<IGetCurrentSessionUseCase, GetCurrentSessionUseCase>();
        services.AddScoped<IGetSessionUseCase, GetSessionUseCase>();
        services.AddScoped<IAnswerQuestionUseCase, AnswerQuestionUseCase>();
        services.AddScoped<IFinishSessionUseCase, FinishSessionUseCase>();

        services.AddScoped<IReadinessQueries, ReadinessQueries>();
        services.AddScoped<IWeakSpotsQueries, WeakSpotsQueries>();
        services.AddScoped<IActivityQueries, ActivityQueries>();

        services.AddOptions<ReviewOptions>().Configure(o =>
        {
            o.DefaultTotalLimit = configuration.GetValue(ReviewOptions.TotalLimitKey, o.DefaultTotalLimit);
            o.DefaultNewLimit = configuration.GetValue(ReviewOptions.NewLimitKey, o.DefaultNewLimit);
        })
        .Validate(
            o => ReviewOptions.IsWithinBounds(o.DefaultTotalLimit) && ReviewOptions.IsWithinBounds(o.DefaultNewLimit),
            $"{ReviewOptions.TotalLimitKey} and {ReviewOptions.NewLimitKey} must be {ReviewOptions.MinLimit}..{ReviewOptions.MaxLimit}.")
        .ValidateOnStart();
        return services;
    }
}
