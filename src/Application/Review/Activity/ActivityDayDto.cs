namespace Spine.Application.Review.Activity;

/// <summary>Answers given on one calendar day in the requested time zone.</summary>
public sealed record ActivityDayDto(DateOnly Date, int Answers);
