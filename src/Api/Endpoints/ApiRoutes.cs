namespace Spine.Api.Endpoints;

/// <summary>
/// Every HTTP route of the API in one place, grouped by feature. Endpoint templates are relative to
/// <see cref="Prefix"/>; health checks are mapped outside FastEndpoints, at the root.
/// </summary>
public static class ApiRoutes
{
    /// <summary>The FastEndpoints route prefix: every endpoint is served under <c>/api</c>.</summary>
    public const string Prefix = "api";

    /// <summary>Every endpoint template served under <see cref="Prefix"/>, each exactly once.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Knowledge.Tree,
        Knowledge.Concept,
        Review.Overview,
        Review.Coverage,
        Review.Readiness,
        Review.WeakSpots,
        Review.Activity,
        Review.Sessions,
        Review.CurrentSession,
        Review.Session,
        Review.Answers,
        Review.Finish,
        Meta.Info,
    ];

    public static class Knowledge
    {
        public const string Tree = "/knowledge/tree";
        public const string Concept = "/knowledge/concepts/{domain}/{topic}/{concept}";
    }

    public static class Review
    {
        public const string Overview = "/review/overview";
        public const string Coverage = "/review/coverage";
        public const string Readiness = "/review/readiness";
        public const string WeakSpots = "/review/weak-spots";
        public const string Activity = "/review/activity";
        public const string Sessions = "/review/sessions";
        public const string CurrentSession = "/review/sessions/current";
        public const string Session = "/review/sessions/{id}";
        public const string Answers = "/review/sessions/{id}/answers";
        public const string Finish = "/review/sessions/{id}/finish";
    }

    public static class Meta
    {
        public const string Info = "/meta";
    }

    public static class Health
    {
        public const string Live = "/health";
        public const string Ready = "/health/ready";
    }
}
