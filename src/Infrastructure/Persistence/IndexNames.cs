namespace Spine.Infrastructure.Persistence;

/// <summary>Database index names the code reacts to (unique-violation handling).</summary>
public static class IndexNames
{
    /// <summary>The partial unique index that closes the double-submit race on <c>attempts</c>.</summary>
    public const string SessionItem = "ix_attempts_session_id_item_id";

    /// <summary>The unique index on <c>review_states (user_id, item_id)</c>: one schedule per learner and item.</summary>
    public const string ReviewStateItem = "ix_review_states_user_id_item_id";
}
