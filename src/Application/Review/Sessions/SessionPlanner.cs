namespace Spine.Application.Review.Sessions;

/// <summary>Pure membership planning (FR-001): due first, then new, bounded by the two limits.</summary>
public static class SessionPlanner
{
    /// <param name="dueIdsOrdered">Due question ids, most overdue first.</param>
    /// <param name="newIdsOrdered">Never-answered question ids in taxonomy order.</param>
    /// <returns>Ordered membership; empty when there is nothing to review.</returns>
    public static string[] Plan(IReadOnlyList<string> dueIdsOrdered, IReadOnlyList<string> newIdsOrdered, int totalLimit, int newLimit)
    {
        ArgumentNullException.ThrowIfNull(dueIdsOrdered);
        ArgumentNullException.ThrowIfNull(newIdsOrdered);
        ArgumentOutOfRangeException.ThrowIfNegative(totalLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(newLimit);

        var dueTaken = Math.Min(dueIdsOrdered.Count, totalLimit);
        var newTaken = Math.Min(newIdsOrdered.Count, Math.Min(newLimit, totalLimit - dueTaken));

        var plan = new string[dueTaken + newTaken];
        for (var i = 0; i < dueTaken; i++)
        {
            plan[i] = dueIdsOrdered[i];
        }

        for (var i = 0; i < newTaken; i++)
        {
            plan[dueTaken + i] = newIdsOrdered[i];
        }

        return plan;
    }

    /// <summary>Weak-mode membership (FR-002): the weak questions in the given order, capped at the total limit.</summary>
    /// <param name="weakIdsOrdered">Question ids whose last answer was wrong, newest miss first.</param>
    public static string[] PlanWeak(IReadOnlyList<string> weakIdsOrdered, int totalLimit)
    {
        ArgumentNullException.ThrowIfNull(weakIdsOrdered);
        ArgumentOutOfRangeException.ThrowIfNegative(totalLimit);

        return [.. weakIdsOrdered.Take(totalLimit)];
    }
}
