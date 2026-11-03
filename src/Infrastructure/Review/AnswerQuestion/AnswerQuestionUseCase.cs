using Spine.Application.Review;
using Spine.Application.Review.AnswerQuestion;
using Spine.Domain.Content;
using Spine.Domain.Progress;
using Spine.Domain.Scheduling;
using Spine.Infrastructure.Persistence;
using Spine.Infrastructure.Review.Sessions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Spine.Infrastructure.Review.AnswerQuestion;

/// <summary>
/// EF implementation of <see cref="IAnswerQuestionUseCase"/>: grades against the English options, records the attempt and
/// moves the review state through <see cref="ISchedulingAlgorithm"/>, all in one retried transaction under the session lock.
/// </summary>
public sealed class AnswerQuestionUseCase(AppDbContext db, ISchedulingAlgorithm algorithm, TimeProvider clock)
    : IAnswerQuestionUseCase
{
    public async Task<AnswerFeedbackDto> ExecuteAsync(
        Guid sessionId, string questionId, int selectedIndex, Language language, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questionId);
        ArgumentOutOfRangeException.ThrowIfNegative(selectedIndex);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = clock.GetUtcNow();
            var userId = LocalLearner.Id;

            await db.LockSessionAsync(sessionId, ct);
            var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                ?? throw new ReviewException(ReviewError.SessionNotFound, $"No session '{sessionId}'.");
            if (session.FinishedAt is not null)
            {
                throw new ReviewException(ReviewError.SessionFinished, $"Session '{sessionId}' was finished at {session.FinishedAt:O}.");
            }

            if (!session.QuestionIds.Contains(questionId, StringComparer.Ordinal))
            {
                throw new ReviewException(ReviewError.QuestionNotInSession, $"Question '{questionId}' is not part of session '{sessionId}'.");
            }

            if (await db.Attempts.AnyAsync(a => a.SessionId == sessionId && a.QuestionId == questionId, ct))
            {
                throw new ReviewException(ReviewError.DuplicateAnswer, $"Question '{questionId}' was already answered in session '{sessionId}'.");
            }

            // FR-011: a question deprecated after the session started is skipped on resume, so it cannot be answered either.
            var deprecated = await db.Questions.AsNoTracking()
                .Where(q => q.Id == questionId)
                .Select(q => q.DeprecatedAt != null
                    || q.Concept!.DeprecatedAt != null
                    || q.Concept.Topic!.DeprecatedAt != null
                    || q.Concept.Topic.Domain!.DeprecatedAt != null)
                .SingleOrDefaultAsync(ct);
            if (deprecated)
            {
                throw new ReviewException(ReviewError.QuestionDeprecated, $"Question '{questionId}' was deprecated after session '{sessionId}' started.");
            }

            // Grading always uses the English options; the requested language only picks the explanation.
            var translations = await db.QuestionTranslations.AsNoTracking()
                .Where(t => t.QuestionId == questionId && (t.Language == Language.En || t.Language == language))
                .Select(t => new { t.Language, t.Options, t.Explanation })
                .ToListAsync(ct);
            var translation = translations.SingleOrDefault(t => t.Language == Language.En)
                ?? throw new InvalidOperationException($"Question '{questionId}' has no English translation.");
            var requested = language == Language.En ? null : translations.SingleOrDefault(t => t.Language == language);
            var mcqOptions = translation.Options ?? throw new InvalidOperationException($"Question '{questionId}' has no options.");
            if (selectedIndex >= mcqOptions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex), selectedIndex, $"Question '{questionId}' has {mcqOptions.Count} options.");
            }

            var correctIndex = IndexOfCorrect(mcqOptions, questionId);
            var isCorrect = selectedIndex == correctIndex;

            db.Attempts.Add(new Attempt
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ItemType = ItemType.Question,
                QuestionId = questionId,
                SessionId = sessionId,
                AnsweredAt = now,
                CreatedAt = now,
                Answer = McqAnswerJson.Serialize(new McqAnswer(selectedIndex, mcqOptions[selectedIndex].Text)),
                IsCorrect = isCorrect,
            });

            var state = await db.ReviewStates.SingleOrDefaultAsync(r => r.UserId == userId && r.QuestionId == questionId, ct);
            ReviewSnapshot snapshot;
            if (state is null)
            {
                state = new ReviewState
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    ItemType = ItemType.Question,
                    QuestionId = questionId,
                    DueAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.ReviewStates.Add(state);
                snapshot = ReviewSnapshot.New(now);
            }
            else
            {
                snapshot = state.ToSnapshot();
            }

            state.Apply(algorithm.Next(snapshot, isCorrect ? ReviewGrade.Good : ReviewGrade.Again, now), algorithm.Name);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: IndexNames.SessionItem or IndexNames.ReviewStateItem,
            })
            {
                // Two submissions raced past the pre-check (the session row lock normally serialises them); the index
                // decided, the client still sees a clean conflict.
                throw new ReviewException(ReviewError.DuplicateAnswer, $"Question '{questionId}' was already answered in session '{sessionId}'.");
            }

            await tx.CommitAsync(ct);
            var served = TranslationPicker.Pick(language, translation.Explanation, mcqOptions, requested?.Explanation, requested?.Options);
            return new AnswerFeedbackDto(
                questionId,
                selectedIndex,
                isCorrect,
                correctIndex,
                served.Text,
                state.DueAt,
                state.IntervalDays,
                served.Language.ToDbValue(),
                served.Fallback);
        });
    }

    private static int IndexOfCorrect(IReadOnlyList<McqOption> mcqOptions, string questionId)
    {
        for (var i = 0; i < mcqOptions.Count; i++)
        {
            if (mcqOptions[i].Correct)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Question '{questionId}' has no correct option.");
    }
}
