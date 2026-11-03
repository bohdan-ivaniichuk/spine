namespace Spine.Application.Review.GetReviewCoverage;

/// <summary>Eligible, seen and due MCQs per non-deprecated topic in manifest order.</summary>
public interface IGetReviewCoverageUseCase
{
    Task<ReviewCoverageDto> ExecuteAsync(CancellationToken ct);
}
