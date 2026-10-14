namespace Spine.Domain.Content;

/// <summary>One of exactly four answer options of a multiple-choice question.</summary>
public sealed record McqOption(string Text, bool Correct);
