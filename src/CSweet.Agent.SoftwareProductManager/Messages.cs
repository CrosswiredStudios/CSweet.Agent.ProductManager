using System.Text.Json;
using CSweet.Agent.SDK;

namespace CSweet.Agent.SoftwareProductManager;

internal static class AgentMessageContextKeys
{
    public const string SenderOrganizationUserId = "senderOrganizationUserId";
    public const string SenderDisplayName = "senderDisplayName";
    public const string SenderEmployeeType = "senderEmployeeType";
    public const string SenderRole = "senderRole";
}

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

public sealed record CommunicationHubDirectoryResponse(
    IReadOnlyList<CommunicationChatResponse> Chats);

public sealed record ReadCommunicationDirectoryRequest { }

public sealed record ModifyCommunicationChatRequest(
    Guid ChatId,
    string Title,
    string? Description,
    bool IsPrivate,
    IReadOnlyList<Guid> ParticipantOrganizationUserIds,
    IReadOnlyList<Guid>? AudienceRoleIds = null,
    IReadOnlyList<Guid>? AudienceWorkstreamIds = null);

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

public sealed record ArchitecturePublicationApproval(
    string ApproverRole,
    string Rationale,
    DateTimeOffset ApprovedAt,
    Guid? SourceConversationId = null,
    Guid? SourceMessageId = null);

public sealed record GuardedArchitecturePublishRequest(
    Guid BoardId,
    JsonElement Design,
    ArchitecturePublicationApproval Approval,
    string IdempotencyKey)
{
    public Guid RepositoryConnectionId { get; init; }
    public string? BaseBranch { get; init; }
    public int FirstSprintSequence { get; init; }
    public Guid AccountableOrganizationUserId { get; init; }
    public Guid DeveloperInstallationId { get; init; }
    public Guid QualityInstallationId { get; init; }
}

public sealed record ArchitecturePublishResponse(
    Guid PlanId,
    Guid EpicId,
    IReadOnlyList<PublishedArchitectureSprint> Sprints,
    IReadOnlyList<PublishedArchitectureTicket> Tickets,
    DateTimeOffset PublishedAt);

public sealed record PublishedArchitectureSprint(int Ordinal, Guid SprintId, string Name);
public sealed record PublishedArchitectureTicket(string Key, Guid ItemId, Guid SprintId, string Kind);

public sealed record GuardedArchitecturePublishResult(
    ArchitecturePublishResponse Publication,
    IReadOnlyList<Guid> ReadyTicketIds);
