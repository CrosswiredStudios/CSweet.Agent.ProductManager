using CSweet.Agent.SDK;

namespace CSweet.Agents.ProductManager;

public sealed record AgentOnboardedEvent(
    Guid OrganizationId,
    Guid AgentOrganizationUserId,
    Guid HiringOrganizationUserId,
    Guid ConversationId,
    DateTimeOffset OccurredAt,
    Guid EventId = default);

public sealed record CompleteAgentOnboardingRequest(Guid EventId);

public sealed record CreateCommunicationChatRequest(
    string? Title,
    string? Description,
    bool IsDirect,
    bool IsPrivate,
    IReadOnlyList<Guid> ParticipantOrganizationUserIds,
    IReadOnlyList<Guid>? AudienceRoleIds = null,
    IReadOnlyList<Guid>? AudienceWorkstreamIds = null);

public sealed record SendCommunicationMessageRequest(
    Guid ChatId,
    string Content,
    string IdempotencyKey);

public sealed record CommunicationHubActionResponse(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    CommunicationChatResponse? Chat = null);

public sealed record CommunicationChatResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsDirect,
    bool IsPrivate,
    bool IsDeletionProtected,
    bool CanManage,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CommunicationParticipantResponse> Participants,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);

public sealed record CommunicationParticipantResponse(
    Guid OrganizationUserId,
    string DisplayName,
    string EmployeeType,
    string Role);

public sealed record ReadCommunicationChatRequest(Guid ChatId);

public sealed record ReadCommunicationChatResponse(
    IReadOnlyList<ReadCommunicationMessageResponse> Messages);

public sealed record ReadCommunicationMessageResponse(
    Guid Id,
    Guid ChatId,
    Guid? SenderOrganizationUserId,
    string Content,
    DateTimeOffset CreatedAt,
    Guid? ChatTurnId);

public sealed record UserMessageReceived(
    Guid ProviderProfileId,
    string ConversationId,
    string UserId,
    string Message,
    IReadOnlyDictionary<string, string>? Context,
    Guid TurnId = default,
    int Attempt = 0,
    Guid MessageId = default);

public sealed record AssistantCapabilityInput(
    Guid ProviderProfileId,
    string ConversationId,
    string Prompt,
    IReadOnlyDictionary<string, string>? Context,
    string? UserId = null,
    Guid MessageId = default,
    Guid ChatTurnId = default);

public sealed record AssistantResponseCreated(
    string ConversationId,
    string Response,
    IReadOnlyList<ProposedAction> ProposedActions,
    DateTimeOffset CreatedAt);

public sealed record ProposedAction(
    string ActionType,
    string Summary,
    string ParametersJson,
    bool RequiresApproval);

public sealed record AssistantResponseChunk(
    string ConversationId,
    int Sequence,
    string Delta,
    bool IsFinal,
    string? Error = null,
    Guid TurnId = default,
    string Kind = "output",
    IReadOnlyDictionary<string, string>? Metadata = null,
    int Attempt = 0);

public sealed record ProductOperatingContext(
    BusinessProfileResponse? BusinessProfile,
    FinancialOperatingProfileResponse? FinancialProfile,
    OrganizationSnapshotResponse? Organization,
    BusinessPatternSearchResponse? Patterns,
    ManagementCycleResponse? ManagementCycle,
    ProductRoleBriefResponse? RoleBrief,
    IReadOnlyList<string> UnavailableCapabilities);
