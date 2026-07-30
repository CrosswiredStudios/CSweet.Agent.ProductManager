using CSweet.Agent.SDK;

namespace CSweet.Agents.ProductManager.Tests;

public sealed class ResourceChangeRoutingTests
{
    [Fact]
    public void ApprovalClaimGuard_ReplacesUnverifiedSubmissionClaim()
    {
        const string draft =
            "I submitted the Lean Technical Spike Team to your manager for approval.";

        var guarded = ProductManagerAgent.EnsureAccurateApprovalStatus(draft, null);

        Assert.Contains("did not create a durable approval request", guarded);
        Assert.Contains("No approval is pending", guarded);
        Assert.DoesNotContain("I submitted", guarded);
    }

    [Fact]
    public void ApprovalClaimGuard_AppendsDurableRequestIdAfterSuccessfulSubmission()
    {
        var requestId = Guid.NewGuid();
        var response = new ResourceChangeRequestResponse(
            requestId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Technical spike",
            "Validate feasibility.",
            1,
            [],
            [],
            [],
            [],
            null,
            "Pending",
            "DeliveredInChat",
            null,
            DateTimeOffset.UtcNow,
            null);

        var guarded = ProductManagerAgent.EnsureAccurateApprovalStatus(
            "I submitted the recommendation for approval.",
            response);

        Assert.Contains(requestId.ToString("D"), guarded);
        Assert.Contains("Pending", guarded);
    }

    [Fact]
    public async Task ManagerTurn_RetainsSourceConversationAndTurn()
    {
        var fixture = new RoutingFixture(managerType: "Agent", sourceIsManager: true);

        await fixture.SubmitAsync();

        Assert.Equal(fixture.SourceConversationId, fixture.Proposal!.ConversationId);
        Assert.Equal(fixture.SourceTurnId, fixture.Proposal.ChatTurnId);
        Assert.Equal(0, fixture.ManagerChatCreateCount);
    }

    [Fact]
    public async Task ManagerTurn_WithLegacyTranscriptWithoutTurnId_RetainsSourceConversationAndTurn()
    {
        var fixture = new RoutingFixture(
            managerType: "Human",
            sourceIsManager: true,
            includeTranscriptTurnId: false);

        await fixture.SubmitAsync();

        Assert.Equal(fixture.SourceConversationId, fixture.Proposal!.ConversationId);
        Assert.Equal(fixture.SourceTurnId, fixture.Proposal.ChatTurnId);
        Assert.Equal(0, fixture.ManagerChatCreateCount);
    }

    [Fact]
    public async Task MissingOptionalCollections_AreNormalizedAndSubmitted()
    {
        var fixture = new RoutingFixture(managerType: "Human", sourceIsManager: true);

        await fixture.SubmitAsync(omitOptionalCollections: true);

        Assert.Empty(fixture.Proposal!.Assumptions);
        Assert.Empty(fixture.Proposal.Constraints);
    }

    [Fact]
    public async Task TeamMetadata_IsBoundedBeforeSubmission()
    {
        var fixture = new RoutingFixture(managerType: "Human", sourceIsManager: true);

        await fixture.SubmitAsync(teamName: new string('T', 200));

        Assert.Equal(160, fixture.Proposal!.TeamName!.Length);
        Assert.Equal("product-team:", fixture.Proposal.TeamKey![..13]);
    }

    [Fact]
    public async Task ExecutiveTurn_WithAgentManager_RoutesToProtectedManagerConversation()
    {
        var fixture = new RoutingFixture(managerType: "Agent", sourceIsManager: false);

        await fixture.SubmitAsync();

        Assert.Equal(fixture.ManagerConversationId, fixture.Proposal!.ConversationId);
        Assert.Equal(Guid.Empty, fixture.Proposal.ChatTurnId);
        Assert.Equal(1, fixture.ManagerChatCreateCount);
    }

    [Fact]
    public async Task ExecutiveTurn_WithHumanManager_FailsWithActionableRoutingMessage()
    {
        var fixture = new RoutingFixture(managerType: "Human", sourceIsManager: false);

        var exception = await Assert.ThrowsAsync<ResourceChangeRoutingException>(
            () => fixture.SubmitAsync());

        Assert.Contains("direct conversation", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(fixture.Proposal);
    }

    [Fact]
    public async Task IdenticalRoleSet_UsesStableIdempotencyKeyAcrossExecutiveRetries()
    {
        var fixture = new RoutingFixture(managerType: "Agent", sourceIsManager: false);

        await fixture.SubmitAsync();
        var firstKey = fixture.Proposal!.IdempotencyKey;
        await fixture.SubmitAsync();

        Assert.Equal(firstKey, fixture.Proposal!.IdempotencyKey);
    }

    private sealed class RoutingFixture
    {
        private readonly AgentRuntimeContext _context;
        private readonly ProductOperatingContext _operatingContext;
        private readonly AssistantCapabilityInput _input;

        public RoutingFixture(
            string managerType,
            bool sourceIsManager,
            bool includeTranscriptTurnId = true)
        {
            var organizationId = Guid.NewGuid();
            var installationId = Guid.NewGuid();
            var productManagerId = Guid.NewGuid();
            var managerId = Guid.NewGuid();
            var sourceSenderId = sourceIsManager ? managerId : Guid.NewGuid();
            SourceConversationId = Guid.NewGuid();
            ManagerConversationId = sourceIsManager ? SourceConversationId : Guid.NewGuid();
            SourceTurnId = Guid.NewGuid();
            var sourceMessageId = Guid.NewGuid();

            var runtime = new AgentTestRuntime()
                .RegisterCapability<ReadCommunicationChatRequest, ReadCommunicationChatResponse>(
                    ProductManagerProfile.ReadCommunicationCapability,
                    (_, _) => Task.FromResult(new ReadCommunicationChatResponse(
                    [
                        new ReadCommunicationMessageResponse(
                            sourceMessageId,
                            SourceConversationId,
                            sourceSenderId,
                            "Finalize the product team.",
                            DateTimeOffset.UtcNow,
                            includeTranscriptTurnId ? SourceTurnId : null)
                    ])))
                .RegisterCapability<CreateCommunicationChatRequest, CommunicationHubActionResponse>(
                    ProductManagerProfile.CreateCommunicationCapability,
                    (_, _) =>
                    {
                        ManagerChatCreateCount++;
                        return Task.FromResult(new CommunicationHubActionResponse(
                            true,
                            null,
                            "Direct chat already exists.",
                            new CommunicationChatResponse(
                                ManagerConversationId,
                                string.Empty,
                                null,
                                true,
                                true,
                                true,
                                false,
                                DateTimeOffset.UtcNow,
                                [],
                                null,
                                null,
                                0)));
                    })
                .RegisterCapability<ResourceChangeProposalRequest, ResourceChangeRequestResponse>(
                    ProductManagerProfile.ProposeResourceChangeCapability,
                    (request, _) =>
                    {
                        Proposal = request;
                        return Task.FromResult(Response(request, organizationId, productManagerId, installationId, managerId));
                    });

            _context = runtime.CreateContext(
                organizationId.ToString("D"),
                installationId.ToString("D"),
                new AgentIdentity(
                    productManagerId.ToString("D"),
                    "Product Manager",
                    null,
                    "Product Manager",
                    null,
                    [],
                    null,
                    managerId.ToString("D"),
                    "Chief of Staff"));
            _operatingContext = new ProductOperatingContext(
                null,
                null,
                new OrganizationSnapshotResponse(
                    organizationId,
                    "Active",
                    [
                        new OrganizationPerson(
                            productManagerId,
                            "Product Manager",
                            "Agent",
                            null,
                            managerId,
                            installationId,
                            true),
                        new OrganizationPerson(
                            managerId,
                            "Chief of Staff",
                            managerType,
                            null,
                            null,
                            managerType == "Agent" ? Guid.NewGuid() : null,
                            true)
                    ],
                    [],
                    [],
                    [],
                    [],
                    DateTimeOffset.UtcNow),
                null,
                null,
                null,
                []);
            _input = new AssistantCapabilityInput(
                Guid.NewGuid(),
                SourceConversationId.ToString("D"),
                "There is no budget and we will use free agents.",
                null,
                Guid.NewGuid().ToString("D"),
                sourceMessageId,
                SourceTurnId);
        }

        public Guid SourceConversationId { get; }
        public Guid ManagerConversationId { get; }
        public Guid SourceTurnId { get; }
        public int ManagerChatCreateCount { get; private set; }
        public ResourceChangeProposalRequest? Proposal { get; private set; }

        public async Task SubmitAsync(
            bool omitOptionalCollections = false,
            string teamName = "Product")
        {
            _ = await ProductManagerAgent.RequestResourceChangeApprovalAsync(
                "Validate and ship the first playable browser game",
                "A compact cross-functional team covers implementation, design, and independent quality.",
                1,
                [
                    new ResourceChangeRole(
                        "web3d",
                        teamName,
                        "Lead Web3D Developer",
                        "Own browser rendering and core mechanics.",
                        1,
                        1,
                        "Now",
                        ["web3d-engineering"],
                        false,
                        null,
                        null)
                ],
                omitOptionalCollections ? null : ["Free agents are acceptable."],
                omitOptionalCollections ? null : ["No paid workforce budget."],
                null,
                _input,
                _operatingContext,
                _context,
                CancellationToken.None);
        }

        private static ResourceChangeRequestResponse Response(
            ResourceChangeProposalRequest request,
            Guid organizationId,
            Guid productManagerId,
            Guid installationId,
            Guid managerId) =>
            new(
                Guid.NewGuid(),
                organizationId,
                productManagerId,
                installationId,
                managerId,
                request.ConversationId,
                request.ChatTurnId,
                request.ProductGoal,
                request.Rationale,
                request.ContextRevision,
                request.Roles,
                request.Roles.Select(x => new ResourceChangeRoleDelta("Add", x, null)).ToList(),
                request.Assumptions,
                request.Constraints,
                request.SupersedesRequestId,
                "Pending",
                "QueuedForManagerAgent",
                null,
                DateTimeOffset.UtcNow,
                null);
    }
}
