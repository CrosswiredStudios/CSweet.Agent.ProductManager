namespace CSweet.Agent.SoftwareProductManager;

public sealed record ProductRoleBriefRequest(
    Guid ProductManagerOrganizationUserId,
    Guid ProductManagerInstallationId,
    Guid SourceEventId,
    string IdempotencyKey);

public sealed record ProductRoleBriefGap(
    string Key,
    string Question,
    string WhyItMatters);

public sealed record ProductRoleBriefResponse(
    string Status,
    Guid ChiefOrganizationUserId,
    Guid ProductManagerOrganizationUserId,
    int ContextRevision,
    string Mandate,
    IReadOnlyList<string> ProductOutcomes,
    IReadOnlyList<string> SuccessMeasures,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> DecisionRights,
    IReadOnlyList<string> KnownTeam,
    IReadOnlyList<ProductRoleBriefGap> MissingInformation,
    DateTimeOffset PreparedAt);

public sealed record ProductTeamRole(
    string Title,
    string Purpose,
    string ReportsTo,
    string Timing,
    int Priority);

public sealed record ProductPlanAlternative(
    string Name,
    string Tradeoff,
    IReadOnlyList<ProductTeamRole> TeamStructure);

public sealed record ProductPlanRequest(
    ProductRoleBriefResponse RoleBrief,
    string Focus,
    Guid SourceEventId,
    string IdempotencyKey);

public sealed record ProductPlanResponse(
    string Recommendation,
    IReadOnlyList<string> ProductStrategy,
    IReadOnlyList<string> RoadmapThemes,
    IReadOnlyList<ProductTeamRole> TeamStructure,
    IReadOnlyList<string> HiringSequence,
    IReadOnlyList<ProductPlanAlternative> Alternatives,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ExecutiveDecisions,
    int ContextRevision,
    DateTimeOffset PreparedAt);

public sealed record ProductPlanReviewRequest(
    Guid ProductManagerOrganizationUserId,
    Guid ProductManagerInstallationId,
    ProductPlanResponse Plan,
    Guid SourceEventId,
    string IdempotencyKey);

public sealed record ProductPlanReviewResponse(
    string Status,
    string PreferredPlan,
    IReadOnlyList<ProductPlanAlternative> Alternatives,
    IReadOnlyList<string> Feedback,
    IReadOnlyList<string> OutstandingDecisions,
    DateTimeOffset ReviewedAt);

public sealed record ProductEscalationRequest(
    Guid ProductManagerOrganizationUserId,
    Guid ProductManagerInstallationId,
    string Topic,
    string Question,
    string WhyItMatters,
    IReadOnlyList<string> Options,
    string? RecommendedOption,
    Guid SourceEventId,
    string IdempotencyKey);

public sealed record ProductEscalationResponse(
    bool Accepted,
    string Status,
    string Message,
    DateTimeOffset AcceptedAt);

public sealed record ProductContextUpdateRequest(
    ProductRoleBriefResponse RoleBrief,
    Guid SourceEventId,
    string IdempotencyKey);

public sealed record ProductContextUpdateResponse(
    bool Acknowledged,
    string State,
    bool PlanRefreshRequired,
    IReadOnlyList<string> MaterialChanges,
    DateTimeOffset AcknowledgedAt);
