using Spine.Domain.Content;

namespace Spine.Infrastructure.Review.Sessions;

/// <summary>Prompt and options of one question translation, as projected for a session card.</summary>
public sealed record SessionQuestionText(string Prompt, IReadOnlyList<McqOption>? Options);
