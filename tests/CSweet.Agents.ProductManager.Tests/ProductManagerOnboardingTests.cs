using System.Runtime.CompilerServices;
using System.Text.Json;
using CSweet.Agent.Contracts.Grpc;
using CSweet.Agent.SDK;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace CSweet.Agents.ProductManager.Tests;

public sealed class ProductManagerOnboardingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task OnboardedEvent_MessagesChief_RequestsBrief_SubmitsPlan_ThenAcknowledges()
    {
        var organizationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var productInstallationId = Guid.NewGuid();
        var chiefId = Guid.NewGuid();
        var chiefInstallationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var broker = new RecordingBroker(
            Snapshot(organizationId, productManagerId, productInstallationId, chiefId, chiefInstallationId),
            ReadyBrief(chiefId, productManagerId));
        var agent = Agent();
        var delivered = Onboarded(
            organizationId,
            productManagerId,
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid());

        await agent.HandleEventAsync(
            delivered,
            new AgentRuntimeContext(
                organizationId.ToString("D"),
                productInstallationId.ToString("D"),
                broker),
            CancellationToken.None);

        var roleBrief = Assert.Single(broker.Requests, x => x.Capability == ProductManagementCapabilities.RoleBrief);
        Assert.Equal($"installation:{chiefInstallationId:D}", roleBrief.TargetAgentId);
        var review = Assert.Single(broker.Requests, x => x.Capability == ProductManagementCapabilities.PlanReview);
        Assert.Equal($"installation:{chiefInstallationId:D}", review.TargetAgentId);
        Assert.DoesNotContain(broker.Requests, x => x.Capability == ProductManagementCapabilities.Escalation);
        var createChat = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CreateCommunicationCapability);
        var createPayload = JsonSerializer.Deserialize<CreateCommunicationChatRequest>(createChat.Payload.Span, JsonOptions)!;
        Assert.Equal([chiefId], createPayload.ParticipantOrganizationUserIds);
        var send = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.SendCommunicationMessageCapability);
        var sendPayload = JsonSerializer.Deserialize<SendCommunicationMessageRequest>(send.Payload.Span, JsonOptions)!;
        Assert.Equal(broker.ManagerChatId, sendPayload.ChatId);
        Assert.Contains("direction", sendPayload.Content, StringComparison.OrdinalIgnoreCase);
        Assert.True(broker.Requests.IndexOf(send) < broker.Requests.IndexOf(roleBrief));
        var acknowledgement = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CompleteOnboardingCapability);
        Assert.True(broker.Requests.IndexOf(acknowledgement) > broker.Requests.IndexOf(review));
    }

    [Fact]
    public async Task OnboardedEvent_WithGap_EscalatesThroughChief_ThenWaits()
    {
        var organizationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var productInstallationId = Guid.NewGuid();
        var chiefId = Guid.NewGuid();
        var chiefInstallationId = Guid.NewGuid();
        var brief = ReadyBrief(chiefId, productManagerId) with
        {
            Status = "AwaitingExecutiveInput",
            MissingInformation =
            [
                new ProductRoleBriefGap(
                    "target-customer",
                    "Which customer segment is the first product for?",
                    "The answer changes discovery and team priorities.")
            ]
        };
        var broker = new RecordingBroker(
            Snapshot(organizationId, productManagerId, productInstallationId, chiefId, chiefInstallationId),
            brief);
        var eventId = Guid.NewGuid();

        await Agent().HandleEventAsync(
            Onboarded(organizationId, productManagerId, eventId, Guid.NewGuid(), Guid.NewGuid()),
            new AgentRuntimeContext(
                organizationId.ToString("D"),
                productInstallationId.ToString("D"),
                broker),
            CancellationToken.None);

        var escalation = Assert.Single(broker.Requests, x => x.Capability == ProductManagementCapabilities.Escalation);
        var payload = JsonSerializer.Deserialize<ProductEscalationRequest>(escalation.Payload.Span, JsonOptions)!;
        Assert.Equal("target-customer", payload.Topic);
        Assert.Equal($"installation:{chiefInstallationId:D}", escalation.TargetAgentId);
        var send = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.SendCommunicationMessageCapability);
        Assert.True(broker.Requests.IndexOf(send) < broker.Requests.IndexOf(escalation));
        Assert.DoesNotContain(broker.Requests, x => x.Capability == ProductManagementCapabilities.PlanReview);
        Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CompleteOnboardingCapability);
    }

    [Fact]
    public async Task OnboardedEvent_WithHumanManager_ReusesHiringConversation_AndWaits()
    {
        var organizationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var productInstallationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var snapshot = SnapshotWithManager(
            organizationId,
            productManagerId,
            productInstallationId,
            new OrganizationPerson(managerId, "CEO", "Human", null, null, null, true));
        var broker = new RecordingBroker(snapshot, null);

        await Agent().HandleEventAsync(
            Onboarded(organizationId, productManagerId, Guid.NewGuid(), managerId, conversationId),
            new AgentRuntimeContext(
                organizationId.ToString("D"),
                productInstallationId.ToString("D"),
                broker),
            CancellationToken.None);

        Assert.DoesNotContain(broker.Requests, x => x.Capability == ProductManagerProfile.CreateCommunicationCapability);
        var send = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.SendCommunicationMessageCapability);
        var payload = JsonSerializer.Deserialize<SendCommunicationMessageRequest>(send.Payload.Span, JsonOptions)!;
        Assert.Equal(conversationId, payload.ChatId);
        Assert.Contains("CEO", payload.Content);
        Assert.DoesNotContain(broker.Requests, x =>
            x.Capability == ProductManagementCapabilities.RoleBrief ||
            x.Capability == ProductManagementCapabilities.PlanReview ||
            x.Capability == ProductManagementCapabilities.Escalation);
        var acknowledgement = Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CompleteOnboardingCapability);
        Assert.True(broker.Requests.IndexOf(acknowledgement) > broker.Requests.IndexOf(send));
    }

    [Fact]
    public async Task OnboardedEvent_WithNonChiefAgentManager_OpensDirectChat_AndWaits()
    {
        var organizationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var productInstallationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var managerInstallationId = Guid.NewGuid();
        var snapshot = SnapshotWithManager(
            organizationId,
            productManagerId,
            productInstallationId,
            new OrganizationPerson(
                managerId,
                "General Manager",
                "Agent",
                null,
                null,
                managerInstallationId,
                true));
        var broker = new RecordingBroker(snapshot, null);

        await Agent().HandleEventAsync(
            Onboarded(organizationId, productManagerId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new AgentRuntimeContext(
                organizationId.ToString("D"),
                productInstallationId.ToString("D"),
                broker),
            CancellationToken.None);

        Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CreateCommunicationCapability);
        Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.SendCommunicationMessageCapability);
        Assert.DoesNotContain(broker.Requests, x =>
            x.Capability == ProductManagementCapabilities.RoleBrief ||
            x.Capability == ProductManagementCapabilities.PlanReview ||
            x.Capability == ProductManagementCapabilities.Escalation);
        Assert.Single(broker.Requests, x => x.Capability == ProductManagerProfile.CompleteOnboardingCapability);
    }

    [Fact]
    public async Task OnboardedEvent_WithoutActiveManager_FailsBeforeAcknowledgement()
    {
        var organizationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var productInstallationId = Guid.NewGuid();
        var snapshot = new OrganizationSnapshotResponse(
            organizationId,
            "Active",
            [new OrganizationPerson(productManagerId, "PM", "Agent", null, null, productInstallationId, true)],
            [],
            [],
            [],
            [],
            DateTimeOffset.UtcNow);
        var broker = new RecordingBroker(snapshot, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Agent().HandleEventAsync(
            Onboarded(organizationId, productManagerId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new AgentRuntimeContext(
                organizationId.ToString("D"),
                productInstallationId.ToString("D"),
                broker),
            CancellationToken.None));

        Assert.DoesNotContain(broker.Requests, x => x.Capability == ProductManagerProfile.CompleteOnboardingCapability);
    }

    private static ProductManagerAgent Agent() => new(
        NullLogger<ProductManagerAgent>.Instance,
        new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));

    private static DeliveredEvent Onboarded(
        Guid organizationId,
        Guid productManagerId,
        Guid eventId,
        Guid hiringUserId,
        Guid conversationId) => new()
    {
        EventId = eventId.ToString("N"),
        EventType = ProductManagerProfile.OnboardedEvent,
        Payload = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(
            new AgentOnboardedEvent(
                organizationId,
                productManagerId,
                hiringUserId,
                conversationId,
                DateTimeOffset.UtcNow),
            JsonOptions)),
        OccurredAt = Timestamp.FromDateTime(DateTime.UtcNow)
    };

    private static OrganizationSnapshotResponse Snapshot(
        Guid organizationId,
        Guid productManagerId,
        Guid productInstallationId,
        Guid chiefId,
        Guid chiefInstallationId) => new(
        organizationId,
        "Active",
        [
            new OrganizationPerson(productManagerId, "Product Manager", "Agent", null, chiefId, productInstallationId, true),
            new OrganizationPerson(chiefId, "Chief of Staff", "Agent", null, null, chiefInstallationId, true)
        ],
        [],
        [],
        [],
        [],
        DateTimeOffset.UtcNow);

    private static OrganizationSnapshotResponse SnapshotWithManager(
        Guid organizationId,
        Guid productManagerId,
        Guid productInstallationId,
        OrganizationPerson manager) => new(
        organizationId,
        "Active",
        [
            new OrganizationPerson(productManagerId, "Product Manager", "Agent", null, manager.Id, productInstallationId, true),
            manager
        ],
        [],
        [],
        [],
        [],
        DateTimeOffset.UtcNow);

    private static ProductRoleBriefResponse ReadyBrief(Guid chiefId, Guid productManagerId) => new(
        "Ready",
        chiefId,
        productManagerId,
        3,
        "Own product outcomes for the first customer segment.",
        ["Validate demand", "Deliver the initial product outcome"],
        ["Activation", "Retention"],
        ["Stay within approved workforce limits"],
        ["Recommend product priorities", "Design the product team"],
        ["Chief of Staff", "Product Manager"],
        [],
        DateTimeOffset.UtcNow);

    private sealed class RecordingBroker(
        OrganizationSnapshotResponse snapshot,
        ProductRoleBriefResponse? brief) : IAgentBrokerClient
    {
        public List<RequestCapability> Requests { get; } = [];
        public Guid ManagerChatId { get; } = Guid.NewGuid();

        public Task<CapabilityResult> InvokeCapabilityAsync(
            RequestCapability request,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            object? response = request.Capability switch
            {
                PlatformCapabilities.OrganizationSnapshotRead => snapshot,
                ProductManagerProfile.CreateCommunicationCapability => new CommunicationHubActionResponse(
                    true,
                    null,
                    "Chat created.",
                    new CommunicationChatResponse(
                        ManagerChatId,
                        "Direct conversation",
                        null,
                        true,
                        true,
                        true,
                        true,
                        DateTimeOffset.UtcNow,
                        [],
                        null,
                        null,
                        0)),
                ProductManagerProfile.SendCommunicationMessageCapability => new { },
                ProductManagementCapabilities.RoleBrief => brief,
                ProductManagementCapabilities.PlanReview => new ProductPlanReviewResponse(
                    "Accepted", "Use the recommended plan.", [], [], [], DateTimeOffset.UtcNow),
                ProductManagementCapabilities.Escalation => new ProductEscalationResponse(
                    true, "Delivered", "The Chief asked the CEO.", DateTimeOffset.UtcNow),
                ProductManagerProfile.CompleteOnboardingCapability => new { },
                _ => null
            };
            return Task.FromResult(response is null
                ? new CapabilityResult
                {
                    RequestId = request.RequestId,
                    Succeeded = false,
                    Error = "Capability unavailable in test."
                }
                : new CapabilityResult
                {
                    RequestId = request.RequestId,
                    Succeeded = true,
                    ContentType = "application/json",
                    Payload = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(response, response.GetType(), JsonOptions))
                });
        }

        public Task StartAsync(RegisterAgent registration, CancellationToken cancellationToken) => Task.CompletedTask;

        public async IAsyncEnumerable<BrokerToAgentMessage> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task PublishEventAsync(
            PublishEvent message,
            string? correlationId = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendCapabilityResultAsync(
            CapabilityResult result,
            string? correlationId = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
