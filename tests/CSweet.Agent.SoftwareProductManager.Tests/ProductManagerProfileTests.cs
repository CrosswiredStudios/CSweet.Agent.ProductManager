using System.Runtime.CompilerServices;
using System.Text.Json;
using CSweet.Agent.SDK;
using CSweet.WorkManagement.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CSweet.Agent.SoftwareProductManager.Tests;

public sealed class ProductManagerProfileTests
{
    [Fact]
    public void Manifest_UsesProductIdentityAndLeastPrivilegeCoordination()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath()));
        var root = document.RootElement;
        Assert.Equal("com.csweet.product-manager", ProductManagerProfile.AgentId);
        Assert.Equal("C-Sweet Software Product Manager", ProductManagerProfile.DefaultDisplayName);
        Assert.Equal(ProductManagerProfile.AgentId, root.GetProperty("id").GetString());
        Assert.Equal(ProductManagerProfile.DefaultDisplayName, root.GetProperty("name").GetString());
        Assert.Equal(ProductManagerProfile.Version, root.GetProperty("version").GetString());
        var provides = root.GetProperty("provides").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).ToHashSet();
        var requires = root.GetProperty("requires").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).ToHashSet();
        var providerCapabilities = new HashSet<string>(StringComparer.Ordinal)
        {
            ProductManagerProfile.SoftwareArchitectureDesignCapability,
            ProductManagerProfile.SoftwareArchitecturePublishCapability
        };
        Assert.All(provides.Concat(requires).Where(capability =>
                !providerCapabilities.Contains(capability!)),
            capability => Assert.Contains(capability!, CapabilityCatalog.All));
        Assert.Contains(ProductManagementCapabilities.Plan, provides);
        Assert.Contains(ProductManagementCapabilities.ContextUpdate, provides);
        Assert.Contains(ProductManagementCapabilities.RoleBrief, requires);
        Assert.Contains(ProductManagementCapabilities.PlanReview, requires);
        Assert.Contains(ProductManagementCapabilities.Escalation, requires);
        Assert.Contains(WorkBoardCapabilities.Create, requires);
        Assert.Contains(ProductManagerProfile.TeamRosterCapability, requires);
        Assert.Contains(ProductManagerProfile.SoftwareArchitectureDesignCapability, requires);
        Assert.Contains(ProductManagerProfile.SoftwareArchitecturePublishCapability, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringRecommendationList, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringRecommendationUpsert, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringWorkflowStage, requires);
        Assert.Contains(ProductManagerProfile.CreateCommunicationCapability, requires);
        Assert.Contains(ProductManagerProfile.SendCommunicationMessageCapability, requires);
        Assert.Contains(ProductManagerProfile.ProposeResourceChangeCapability, requires);
        Assert.Contains(PlatformCapabilities.ResourceChangeRead, requires);
        Assert.Contains(AgentLifecycleCapabilities.CompleteOnboarding, requires);
        Assert.Contains(MemoryCapabilities.BusinessRead, requires);
    }

    [Fact]
    public async Task Manifest_LoadsAndMatchesTheStandaloneAuthoringContract()
    {
        var manifestPath = ManifestPath();
        var manifest = await AgentManifestLoader.LoadAsync(manifestPath, CancellationToken.None);

        Assert.Equal(ProductManagerProfile.AgentId, manifest.Id);
        Assert.Equal(ProductManagerProfile.Version, manifest.Version);
        Assert.Equal(1, manifest.Runtime.MaximumConcurrentJobs);
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(manifestPath)!, "AGENTS.md")));

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        var root = document.RootElement;
        Assert.All(root.GetProperty("provides").EnumerateArray(), capability =>
        {
            Assert.False(capability.GetProperty("inputSchema").GetProperty("additionalProperties").GetBoolean());
            Assert.False(capability.GetProperty("outputSchema").GetProperty("additionalProperties").GetBoolean());
        });
        Assert.Equal(
            [
                ProductManagerProfile.OnboardedEvent,
                ProductManagerProfile.UserMessageReceivedEvent,
                ManagementEvents.ReviewDue,
                ManagementEvents.ResourceChangeDecided,
                ProductManagerProfile.RecommendationFulfilledEvent
            ],
            root.GetProperty("events").GetProperty("subscribes").EnumerateArray()
                .Select(item => item.GetString()!).ToArray());
        Assert.Equal(
            ["llmProviderId", "llmModel", "responseTone"],
            root.GetProperty("configuration").EnumerateArray()
                .Select(item => item.GetProperty("key").GetString()!).ToArray());
        Assert.All(
            root.GetProperty("configuration").EnumerateArray(),
            field => Assert.False(string.IsNullOrWhiteSpace(
                field.GetProperty("description").GetString())));
        var manifestTone = root.GetProperty("configuration").EnumerateArray()
            .Single(field => field.GetProperty("key").GetString() == "responseTone");
        Assert.Equal(
            ["concise", "balanced", "detailed"],
            manifestTone.GetProperty("options").EnumerateArray()
                .Select(option => option.GetProperty("value").GetString()!).ToArray());

        var project = await File.ReadAllTextAsync(Path.Combine(
            Path.GetDirectoryName(manifestPath)!,
            "src",
            "CSweet.Agent.SoftwareProductManager",
            "CSweet.Agent.SoftwareProductManager.csproj"));
        Assert.Contains("CSweet.Agent.SDK\" Version=\"2.7.0", project, StringComparison.Ordinal);
        Assert.Contains("<ProjectReference", project, StringComparison.Ordinal);
        Assert.Contains($"<Version>{ProductManagerProfile.Version}</Version>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemPrompt_EnforcesProductAndChiefBoundaries()
    {
        Assert.Contains("customer discovery", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("roadmap", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("success measures", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("at most two", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("directly message your managing employee", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approved organization and relationship memory", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never open with a generic readiness message", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CEO, Chief of Staff, another human, or another agent", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never maintain the Chief's hiring backlog", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not present a finalized role list", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("routes the request to your authoritative manager", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not provide technical architecture", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("primary startup goal", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kanban board", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resubmit", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            ProductManagerProfile.SoftwareArchitectureDesignCapability,
            ProductManagerProfile.SystemPrompt,
            StringComparison.Ordinal);
        Assert.Contains(
            ProductManagerProfile.SoftwareArchitecturePublishCapability,
            ProductManagerProfile.SystemPrompt,
            StringComparison.Ordinal);
        Assert.Contains("direct agent conversation", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approval boundary", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SoftwareArchitectProviderCapabilitiesAreVisibleAsGovernedModelTools()
    {
        var runtime = new AgentTestRuntime()
            .RegisterCapability<object, object>(
                ProductManagerProfile.SoftwareArchitectureDesignCapability,
                (_, _) => Task.FromResult<object>(new { }),
                modelVisible: true)
            .RegisterCapability<object, object>(
                ProductManagerProfile.SoftwareArchitecturePublishCapability,
                (_, _) => Task.FromResult<object>(new { }),
                modelVisible: true);

        var tools = await runtime.CreateContext().GetModelToolsAsync();
        var names = tools.OfType<AIFunctionDeclaration>().Select(x => x.Name).ToArray();

        Assert.Contains("software_architecture_design_v1", names);
        Assert.Contains("software_architecture_publish_plan_v1", names);
    }

    [Fact]
    public void ContextualOnboardingFallback_IdentifiesTheManagedDeliverableAndTeamApprovalNextStep()
    {
        var organizationId = Guid.NewGuid();
        var profile = new BusinessProfileResponse(
            organizationId,
            "Super Awesome Games",
            "Game Studio",
            "Games",
            "Build browser games.",
            "Make classic games accessible on the web.",
            "Validation",
            ["Classic game fans"],
            ["A browser-based Star Fox 64-inspired game"],
            null,
            ["United States"],
            null,
            [],
            [],
            null,
            "UTC",
            3,
            0.8m,
            new Dictionary<string, ProfileFieldProvenance>());
        var finance = new FinancialOperatingProfileResponse(
            organizationId,
            "USD",
            null,
            null,
            null,
            null,
            20_000m,
            null,
            3,
            "Approval",
            2);
        var organization = new OrganizationSnapshotResponse(
            organizationId,
            "Active",
            [],
            [],
            [new OrganizationObjective(
                Guid.NewGuid(),
                "Deliver a playable browser prototype",
                "Validate the core gameplay loop.",
                "Active",
                null)],
            [],
            [],
            DateTimeOffset.UtcNow);
        var context = new ProductOperatingContext(profile, finance, organization, null, null, null, []);

        var message = ProductManagerOrchestrator.BuildManagerDirectionRequest(context, "Chief of Staff");

        Assert.Contains("Deliver a playable browser prototype", message, StringComparison.Ordinal);
        Assert.Contains("Classic game fans", message, StringComparison.Ordinal);
        Assert.Contains("smallest cross-functional team", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("one proposal for your approval", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ready to begin", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Please confirm my mandate", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Onboarding_UsesTheConfiguredModelForTheFirstManagerMessage()
    {
        var organizationId = Guid.NewGuid();
        var installationId = Guid.NewGuid();
        var productManagerId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var providerProfileId = Guid.NewGuid();
        var onboardingEventId = Guid.NewGuid();
        var workItemId = Guid.NewGuid();
        var generatedOpening = "I’m managing the playable browser prototype for classic game fans. I’m now shaping the smallest team and will submit it for approval.";
        SendCommunicationMessageRequest? sentMessage = null;
        CompleteAgentOnboardingRequest? completionRequest = null;
        var profile = new BusinessProfileResponse(
            organizationId,
            "Super Awesome Games",
            "Game Studio",
            "Games",
            "Build browser games.",
            "Make classic games accessible on the web.",
            "Validation",
            ["Classic game fans"],
            ["A browser-based Star Fox 64-inspired game"],
            null,
            ["United States"],
            null,
            [],
            [],
            null,
            "UTC",
            3,
            0.8m,
            new Dictionary<string, ProfileFieldProvenance>());
        var organization = new OrganizationSnapshotResponse(
            organizationId,
            "Active",
            [
                new OrganizationPerson(
                    productManagerId,
                    ProductManagerProfile.DefaultDisplayName,
                    "Agent",
                    null,
                    managerId,
                    installationId,
                    true),
                new OrganizationPerson(managerId, "CEO", "Human", null, null, null, true)
            ],
            [],
            [new OrganizationObjective(
                Guid.NewGuid(),
                "Deliver a playable browser prototype",
                "Validate the core gameplay loop.",
                "Active",
                null)],
            [],
            [],
            DateTimeOffset.UtcNow);
        var runtime = new AgentTestRuntime()
            .RegisterCapability<JsonElement, BusinessProfileResponse>(
                PlatformCapabilities.BusinessProfileRead,
                (_, _) => Task.FromResult(profile))
            .RegisterCapability<JsonElement, OrganizationSnapshotResponse>(
                PlatformCapabilities.OrganizationSnapshotRead,
                (_, _) => Task.FromResult(organization))
            .RegisterCapability<SendCommunicationMessageRequest, CommunicationHubActionResponse>(
                ProductManagerProfile.SendCommunicationMessageCapability,
                (request, _) =>
                {
                    sentMessage = request;
                    return Task.FromResult(new CommunicationHubActionResponse(true, null, "sent"));
                })
            .RegisterCapability<CompleteAgentOnboardingRequest, JsonElement>(
                AgentLifecycleCapabilities.CompleteOnboarding,
                (request, _) =>
                {
                    completionRequest = request;
                    return Task.FromResult(JsonSerializer.SerializeToElement(new { completed = true }));
                });
        var chatClient = new CapturingChatClient(generatedOpening);
        var agent = new ProductManagerAgent(
            new TestLlmClientFactory(chatClient),
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));
        var context = runtime.CreateContext(
            organizationId.ToString("D"),
            installationId.ToString("D"));
        var configurationResult = await agent.ExecuteCapabilityAsync(
            new AgentCapabilityRequest(
                Guid.NewGuid(),
                AgentConfigurationCapabilities.Update,
                JsonSerializer.SerializeToElement(new UpdateAgentConfigurationRequest(
                    new Dictionary<string, JsonElement>
                    {
                        ["llmProviderId"] = JsonSerializer.SerializeToElement(providerProfileId.ToString("D")),
                        ["llmModel"] = JsonSerializer.SerializeToElement("test-model")
                    }))),
            context,
            CancellationToken.None);
        Assert.True(configurationResult.Succeeded);

        await agent.HandleEventAsync(
            new AgentEventEnvelope(
                workItemId,
                onboardingEventId,
                ProductManagerProfile.OnboardedEvent,
                JsonSerializer.SerializeToElement(new AgentOnboardedEvent(
                    organizationId,
                    productManagerId,
                    managerId,
                    conversationId,
                    DateTimeOffset.UtcNow)),
                DateTimeOffset.UtcNow,
                Guid.NewGuid().ToString("N")),
            context,
            CancellationToken.None);

        Assert.NotNull(sentMessage);
        Assert.Equal(conversationId, sentMessage.ChatId);
        Assert.Equal(generatedOpening, sentMessage.Content);
        Assert.Contains("Super Awesome Games", chatClient.Prompt, StringComparison.Ordinal);
        Assert.Contains("Deliver a playable browser prototype", chatClient.Prompt, StringComparison.Ordinal);
        Assert.Contains("approved C-Sweet organization", chatClient.Prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not send a generic welcome", chatClient.Prompt, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(completionRequest);
        Assert.Equal(onboardingEventId, completionRequest.EventId);
        Assert.NotEqual(workItemId, completionRequest.EventId);
    }

    [Fact]
    public async Task Configuration_DescribesEveryFieldAndRejectsUnsupportedTone()
    {
        var agent = new ProductManagerAgent(
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));
        var context = new AgentTestRuntime().CreateContext();
        var describe = await agent.ExecuteCapabilityAsync(
            new AgentCapabilityRequest(
                Guid.NewGuid(),
                AgentConfigurationCapabilities.Describe,
                JsonSerializer.SerializeToElement(new { })),
            context,
            CancellationToken.None);

        Assert.True(describe.Succeeded);
        var schema = describe.Value!.Value.Deserialize<AgentConfigurationSchemaResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(schema);
        Assert.Equal(
            ["llmProviderId", "llmModel", "responseTone"],
            schema.Fields.Select(field => field.Key).ToArray());
        var tone = schema.Fields.Single(field => field.Key == "responseTone");
        Assert.Equal(
            ["concise", "balanced", "detailed"],
            tone.Options!.Select(option => option.Value).ToArray());
        Assert.Equal("concise", schema.Settings["responseTone"].GetString());

        var invalid = await agent.ExecuteCapabilityAsync(
            new AgentCapabilityRequest(
                Guid.NewGuid(),
                AgentConfigurationCapabilities.Update,
                JsonSerializer.SerializeToElement(new UpdateAgentConfigurationRequest(
                    new Dictionary<string, JsonElement>
                    {
                        ["responseTone"] = JsonSerializer.SerializeToElement("Blunt")
                    }))),
            context,
            CancellationToken.None);

        Assert.False(invalid.Succeeded);
        Assert.Contains("must be one of", invalid.Error, StringComparison.OrdinalIgnoreCase);

        var valid = await agent.ExecuteCapabilityAsync(
            new AgentCapabilityRequest(
                Guid.NewGuid(),
                AgentConfigurationCapabilities.Update,
                JsonSerializer.SerializeToElement(new UpdateAgentConfigurationRequest(
                    new Dictionary<string, JsonElement>
                    {
                        ["llmProviderId"] = JsonSerializer.SerializeToElement(Guid.NewGuid().ToString("D")),
                        ["llmModel"] = JsonSerializer.SerializeToElement("model"),
                        ["responseTone"] = JsonSerializer.SerializeToElement("concise")
                    }))),
            context,
            CancellationToken.None);

        Assert.True(valid.Succeeded);
    }

    [Fact]
    public void Revision_SequencesImmediateRolesToTheAuthoritativeConcurrentHireCap()
    {
        var roles = new[]
        {
            Role("architecture", "Software Architect", 1, "Now"),
            Role("development", "Software Developer", 2, "Now"),
            Role("quality", "Software QA", 3, "Now")
        };
        var finance = new FinancialOperatingProfileResponse(
            Guid.NewGuid(), "USD", null, null, null, null, null, null, 1, "Approval", 7);

        var revised = ProductManagerAgent.ReviseRolesForAuthoritativeConstraints(roles, finance);

        Assert.Equal("Now", revised[0].Timing);
        Assert.Equal("Next", revised[1].Timing);
        Assert.Equal("Next", revised[2].Timing);
        Assert.Equal(
            ["Software Architect", "Software Developer", "Software QA"],
            revised.Select(x => x.Title).ToArray());
    }

    [Fact]
    public void ProductBoardName_IsAppropriateStableAndWithinPlatformLimit()
    {
        var name = ProductManagerAgent.BuildProductBoardName(new string('x', 300));

        Assert.EndsWith(" - Product Team", name, StringComparison.Ordinal);
        Assert.True(name.Length <= 160);
    }

    [Fact]
    public async Task ApprovedTeam_CreatesIdempotentBoardAndAcknowledgesManager()
    {
        var organizationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        CreateWorkBoardRequest? boardRequest = null;
        ConfigureWorkBoardColumnsRequest? columnsRequest = null;
        ConfigureSoftwareOrchestrationTemplateRequest? templateRequest = null;
        SendCommunicationMessageRequest? messageRequest = null;
        var response = ResourceChange(
            requestId,
            organizationId,
            conversationId,
            "Validate the first customer workflow",
            "Approved");
        var runtime = new AgentTestRuntime()
            .RegisterCapability<ResourceChangeReadRequest, ResourceChangeReadResponse>(
                PlatformCapabilities.ResourceChangeRead,
                (_, _) => Task.FromResult(new ResourceChangeReadResponse([response])))
            .RegisterCapability<CreateWorkBoardRequest, WorkBoardSummary>(
                WorkBoardCapabilities.Create,
                (request, _) =>
                {
                    boardRequest = request;
                    return Task.FromResult(new WorkBoardSummary(
                        Guid.NewGuid(), request.Name, request.Description ?? string.Empty,
                        false, false, 1, [WorkBoardCapabilities.Create]));
                })
            .RegisterCapability<ConfigureWorkBoardColumnsRequest, WorkBoardDetail>(
                WorkBoardCapabilities.ConfigureColumns,
                (request, _) =>
                {
                    columnsRequest = request;
                    return Task.FromResult(new WorkBoardDetail(
                        new WorkBoardSummary(
                            request.BoardId, "Software", "", false, false, 2,
                            [WorkBoardCapabilities.Read, WorkBoardCapabilities.ConfigureColumns]),
                        request.Columns.Select((column, index) => new WorkBoardColumn(
                            Guid.NewGuid(), column.Name, column.Category, index,
                            column.WipPolicy, column.WipLimit)).ToList(),
                        []));
                })
            .RegisterCapability<ConfigureSoftwareOrchestrationTemplateRequest, WorkOrchestrationPolicyRevision>(
                WorkOrchestrationCapabilities.ConfigureSoftwareTemplate,
                (request, _) =>
                {
                    templateRequest = request;
                    return Task.FromResult(new WorkOrchestrationPolicyRevision(
                        Guid.NewGuid(), Guid.NewGuid(), request.BoardId, 1, "Software delivery",
                        "ready", request.MergeMode,
                        new WorkOrchestrationConcurrencyLimits(100, 25, 10, 5, 1),
                        [], [], true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
                })
            .RegisterCapability<SendCommunicationMessageRequest, CommunicationHubActionResponse>(
                ProductManagerProfile.SendCommunicationMessageCapability,
                (request, _) =>
                {
                    messageRequest = request;
                    return Task.FromResult(new CommunicationHubActionResponse(true, null, "sent"));
                });
        var context = runtime.CreateContext(
            organizationId.ToString("D"),
            response.RequesterInstallationId.ToString("D"));
        var agent = new ProductManagerAgent(
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));

        await agent.HandleResourceChangeDecisionAsync(
            new AgentEventEnvelope(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ManagementEvents.ResourceChangeDecided,
                JsonSerializer.SerializeToElement(new ResourceChangeDecisionEvent(
                    requestId,
                    organizationId,
                    response.RequesterOrganizationUserId,
                    response.ManagerOrganizationUserId,
                    "Approved",
                    DateTimeOffset.UtcNow)),
                DateTimeOffset.UtcNow),
            context,
            CancellationToken.None);

        Assert.NotNull(boardRequest);
        Assert.Equal(
            $"product-team-board:{response.RequesterOrganizationUserId:N}",
            boardRequest.IdempotencyKey);
        Assert.Contains("Product Team", boardRequest.Name, StringComparison.Ordinal);
        Assert.Equal(
            ["Backlog", "Ready For Development", "In Development", "Dev Complete", "In Testing", "Ready To Merge", "Done"],
            columnsRequest!.Columns.Select(x => x.Name));
        Assert.NotNull(templateRequest);
        Assert.Equal(3, templateRequest.MaximumQualityCycles);
        Assert.NotNull(messageRequest);
        Assert.Contains("kanban board", messageRequest.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevisionRequested_ResubmitsCompleteTeamAndSupersedesReviewedRequest()
    {
        var organizationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var response = ResourceChange(
            requestId,
            organizationId,
            Guid.NewGuid(),
            "Validate the first customer workflow",
            "RevisionRequested") with
        {
            DecisionComment = "Start only one hire at a time.",
            Roles =
            [
                Role("design", "Product Designer", 1, "Now"),
                Role("engineering", "Product Engineer", 2, "Now")
            ]
        };
        response = response with
        {
            Deltas = response.Roles
                .Select(role => new ResourceChangeRoleDelta("Add", role, null))
                .ToList()
        };
        ResourceChangeProposalRequest? revisedProposal = null;
        var finance = new FinancialOperatingProfileResponse(
            organizationId, "USD", null, null, null, null, null, null, 1, "Approval", 2);
        var runtime = new AgentTestRuntime()
            .RegisterCapability<ResourceChangeReadRequest, ResourceChangeReadResponse>(
                PlatformCapabilities.ResourceChangeRead,
                (_, _) => Task.FromResult(new ResourceChangeReadResponse([response])))
            .RegisterCapability<JsonElement, FinancialOperatingProfileResponse>(
                PlatformCapabilities.FinanceProfileRead,
                (_, _) => Task.FromResult(finance))
            .RegisterCapability<ResourceChangeProposalRequest, ResourceChangeRequestResponse>(
                PlatformCapabilities.ResourceChangePropose,
                (request, _) =>
                {
                    revisedProposal = request;
                    return Task.FromResult(response);
                })
            .RegisterCapability<SendCommunicationMessageRequest, CommunicationHubActionResponse>(
                ProductManagerProfile.SendCommunicationMessageCapability,
                (_, _) => Task.FromResult(new CommunicationHubActionResponse(true, null, "sent")));
        var context = runtime.CreateContext(
            organizationId.ToString("D"),
            response.RequesterInstallationId.ToString("D"));
        var agent = new ProductManagerAgent(
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));

        await agent.HandleResourceChangeDecisionAsync(
            new AgentEventEnvelope(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ManagementEvents.ResourceChangeDecided,
                JsonSerializer.SerializeToElement(new ResourceChangeDecisionEvent(
                    requestId,
                    organizationId,
                    response.RequesterOrganizationUserId,
                    response.ManagerOrganizationUserId,
                    "RevisionRequested",
                    DateTimeOffset.UtcNow)),
                DateTimeOffset.UtcNow),
            context,
            CancellationToken.None);

        Assert.NotNull(revisedProposal);
        Assert.Equal(requestId, revisedProposal.SupersedesRequestId);
        Assert.Equal(["Now", "Next"], revisedProposal.Roles.Select(role => role.Timing).ToArray());
    }

    [Fact]
    public async Task RejectedTeam_AsksManagerForOneFocusedRefinement()
    {
        var organizationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var response = ResourceChange(
            requestId,
            organizationId,
            Guid.NewGuid(),
            "Validate the first customer workflow",
            "Rejected") with
        {
            DecisionComment = "The proposed team is too broad."
        };
        SendCommunicationMessageRequest? messageRequest = null;
        var runtime = new AgentTestRuntime()
            .RegisterCapability<ResourceChangeReadRequest, ResourceChangeReadResponse>(
                PlatformCapabilities.ResourceChangeRead,
                (_, _) => Task.FromResult(new ResourceChangeReadResponse([response])))
            .RegisterCapability<SendCommunicationMessageRequest, CommunicationHubActionResponse>(
                ProductManagerProfile.SendCommunicationMessageCapability,
                (request, _) =>
                {
                    messageRequest = request;
                    return Task.FromResult(new CommunicationHubActionResponse(true, null, "sent"));
                });
        var context = runtime.CreateContext(
            organizationId.ToString("D"),
            response.RequesterInstallationId.ToString("D"));
        var agent = new ProductManagerAgent(
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));

        await agent.HandleResourceChangeDecisionAsync(
            new AgentEventEnvelope(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ManagementEvents.ResourceChangeDecided,
                JsonSerializer.SerializeToElement(new ResourceChangeDecisionEvent(
                    requestId,
                    organizationId,
                    response.RequesterOrganizationUserId,
                    response.ManagerOrganizationUserId,
                    "Rejected",
                    DateTimeOffset.UtcNow)),
                DateTimeOffset.UtcNow),
            context,
            CancellationToken.None);

        Assert.NotNull(messageRequest);
        Assert.Contains("The proposed team is too broad.", messageRequest.Content, StringComparison.Ordinal);
        Assert.Contains("What single outcome, role, or constraint", messageRequest.Content, StringComparison.Ordinal);
        Assert.EndsWith("?", messageRequest.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecommendationFulfilled_ReassessesOnlyItsOwnApprovedPlan()
    {
        var organizationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var response = ResourceChange(
            requestId,
            organizationId,
            Guid.NewGuid(),
            "Deliver the MVP",
            "Approved");
        SendCommunicationMessageRequest? messageRequest = null;
        var messageRequests = new List<SendCommunicationMessageRequest>();
        var runtime = new AgentTestRuntime()
            .RegisterCapability<ResourceChangeReadRequest, ResourceChangeReadResponse>(
                PlatformCapabilities.ResourceChangeRead,
                (request, _) => Task.FromResult(new ResourceChangeReadResponse(
                    request.RequestId == requestId ? [response] : [])))
            .RegisterCapability<TeamRosterRequest, TeamRosterResponse>(
                ProductManagerProfile.TeamRosterCapability,
                (_, _) => Task.FromResult(new TeamRosterResponse(new AgentTeamContext(
                    Guid.NewGuid().ToString("D"),
                    "product",
                    "Product Team",
                    1,
                    response.RequesterOrganizationUserId.ToString("D"),
                    "Product Manager",
                    [],
                    [new TeamRoleCoverage("Product Engineer", 1)],
                    1,
                    false))))
            .RegisterCapability<SendCommunicationMessageRequest, CommunicationHubActionResponse>(
                ProductManagerProfile.SendCommunicationMessageCapability,
                (request, _) =>
                {
                    messageRequest = request;
                    messageRequests.Add(request);
                    return Task.FromResult(new CommunicationHubActionResponse(true, null, "sent"));
                });
        var context = runtime.CreateContext(
            organizationId.ToString("D"),
            response.RequesterInstallationId.ToString("D"));
        var agent = new ProductManagerAgent(
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));

        await agent.HandleHiringRecommendationFulfilledAsync(
            RecommendationFulfilled(organizationId, Guid.NewGuid()),
            context,
            CancellationToken.None);
        Assert.Null(messageRequest);

        var ownEvent = RecommendationFulfilled(organizationId, requestId);
        await agent.HandleHiringRecommendationFulfilledAsync(ownEvent, context, CancellationToken.None);
        await agent.HandleHiringRecommendationFulfilledAsync(ownEvent, context, CancellationToken.None);

        Assert.NotNull(messageRequest);
        Assert.Equal(response.ConversationId, messageRequest.ChatId);
        Assert.Equal($"hiring-recommendation-fulfilled:{ownEvent.EventId:N}:product-manager", messageRequest.IdempotencyKey);
        Assert.Contains("Product Engineer", messageRequest.Content, StringComparison.Ordinal);
        Assert.Contains("covers every planned role", messageRequest.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, messageRequests.Count);
        Assert.Single(messageRequests.Select(request => request.IdempotencyKey).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void ProductPlan_HasPreferredCourse_TwoAlternatives_AndHiringOrder()
    {
        var brief = new ProductRoleBriefResponse(
            "Ready", Guid.NewGuid(), Guid.NewGuid(), 4, "Own validation",
            ["Validate the first customer problem"], ["Activation"], [], [], [], [], DateTimeOffset.UtcNow);
        var profile = new BusinessProfileResponse(
            Guid.NewGuid(), "Trailwise", "Marketplace", "Outdoor recreation", null, null, "Validation",
            ["New outdoor enthusiasts"], ["Guided trip bookings"], "Commission", ["US"], null, [], [], null,
            "UTC", 4, 0.8m, new Dictionary<string, ProfileFieldProvenance>());
        var context = new ProductOperatingContext(profile, null, null, null, null, brief, []);

        var plan = ProductManagerOrchestrator.BuildProductPlan(
            new ProductPlanRequest(brief, "Initial product team", Guid.NewGuid(), "plan-1"),
            context);

        Assert.False(string.IsNullOrWhiteSpace(plan.Recommendation));
        Assert.Equal(2, plan.Alternatives.Count);
        Assert.NotEmpty(plan.TeamStructure);
        Assert.Equal(
            plan.TeamStructure.Select(x => x.Priority).Order().ToArray(),
            plan.TeamStructure.Select(x => x.Priority).ToArray());
        Assert.Equal(
            ["Software Architect", "Software Developer", "Software QA"],
            plan.TeamStructure.Take(3).Select(x => x.Title).ToArray());
        Assert.All(plan.TeamStructure.Take(3), role => Assert.Equal("Now", role.Timing));
        Assert.All(plan.Alternatives, alternative => Assert.Equal(
            ["Software Architect", "Software Developer", "Software QA"],
            alternative.TeamStructure.Take(3).Select(x => x.Title).ToArray()));
        Assert.All(plan.TeamStructure, role => Assert.Equal(ProductManagerProfile.DefaultDisplayName, role.ReportsTo));
        Assert.NotEmpty(plan.HiringSequence);
        Assert.NotEmpty(plan.Assumptions);
    }

    [Fact]
    public void DeliveryChatParticipants_IncludeEveryActiveMemberAndReportingManagerOnce()
    {
        var productManagerId = Guid.NewGuid();
        var reportingManagerId = Guid.NewGuid();
        var architectId = Guid.NewGuid();
        var developerId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var team = new AgentTeamContext(
            Guid.NewGuid().ToString("D"),
            "SOFTWARE",
            "Software",
            1,
            productManagerId.ToString("D"),
            "Product Manager",
            [
                new AgentTeammate(productManagerId.ToString("D"), "Product Manager", "Agent", null, "Software Product Manager", "Self", "Active"),
                new AgentTeammate(architectId.ToString("D"), "Architect", "Agent", null, "Software Architect", "Peer", "Active"),
                new AgentTeammate(developerId.ToString("D"), "Developer", "Agent", null, "Software Developer", "Peer", "Active"),
                new AgentTeammate(inactiveId.ToString("D"), "Former QA", "Agent", null, "Software QA", "Peer", "Inactive")
            ],
            [],
            4,
            false);

        var participants = ProductManagerAgent.BuildDeliveryChatParticipants(
            team, productManagerId, reportingManagerId);

        Assert.Equal(4, participants.Count);
        Assert.Contains(productManagerId, participants);
        Assert.Contains(reportingManagerId, participants);
        Assert.Contains(architectId, participants);
        Assert.Contains(developerId, participants);
        Assert.DoesNotContain(inactiveId, participants);
        Assert.Equal(participants.Count, participants.Distinct().Count());
    }

    [Fact]
    public void FirstSprintReadiness_SelectsOnlyStoriesAndTasksFromLowestOrdinalSprint()
    {
        var firstSprintId = Guid.NewGuid();
        var laterSprintId = Guid.NewGuid();
        var firstStory = new PublishedArchitectureTicket("STORY-1", Guid.NewGuid(), firstSprintId, WorkItemKinds.Story);
        var firstTask = new PublishedArchitectureTicket("TASK-1", Guid.NewGuid(), firstSprintId, WorkItemKinds.Task);
        var firstEpic = new PublishedArchitectureTicket("EPIC-1", Guid.NewGuid(), firstSprintId, WorkItemKinds.Epic);
        var laterStory = new PublishedArchitectureTicket("STORY-2", Guid.NewGuid(), laterSprintId, WorkItemKinds.Story);
        var publication = new ArchitecturePublishResponse(
            Guid.NewGuid(),
            firstEpic.ItemId,
            [
                new PublishedArchitectureSprint(2, laterSprintId, "Sprint 2"),
                new PublishedArchitectureSprint(1, firstSprintId, "Sprint 1")
            ],
            [firstStory, firstTask, firstEpic, laterStory],
            DateTimeOffset.UtcNow);

        var ready = ProductManagerAgent.SelectFirstSprintReadyTickets(publication);

        Assert.Equal([firstStory.ItemId, firstTask.ItemId], ready.Select(x => x.ItemId).ToArray());
    }

    [Theory]
    [InlineData(
        "I attempted to submit the team for approval, but the request was blocked by the platform.",
        true)]
    [InlineData(
        "I submitted the team for approval.",
        true)]
    [InlineData(
        "I cannot submit a team until the product goal is defined.",
        false)]
    public void ApprovalActionDetection_RequiresEvidenceForAttemptAndSuccessClaims(
        string response,
        bool expected)
    {
        Assert.Equal(expected, ProductManagerAgent.ClaimsApprovalAction(response));
    }

    [Fact]
    public void ApprovalStatus_RemovesInventedPlatformRejectionWhenNoToolRan()
    {
        var response = ProductManagerAgent.EnsureAccurateApprovalStatus(
            "I attempted to submit the team for approval, but the request was blocked by the platform.",
            toolResult: null);

        Assert.Contains("no durable approval action was attempted", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No approval is pending", response, StringComparison.Ordinal);
        Assert.DoesNotContain("blocked by the platform", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApprovalStatus_UsesTheActualPlatformFailureFromTheTool()
    {
        var response = ProductManagerAgent.EnsureAccurateApprovalStatus(
            "The request failed.",
            ResourceChangeApprovalToolResult.Failure(
                "The platform rejected the approval request: The proposal must originate from a current manager turn."));

        Assert.Contains(
            "The proposal must originate from a current manager turn.",
            response,
            StringComparison.Ordinal);
        Assert.Contains("No approval is pending", response, StringComparison.Ordinal);
    }

    [Fact]
    public void ApprovalStatus_AppendsTheDurableRequestIdAfterSuccess()
    {
        var request = ResourceChange(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Deliver the MVP",
            "Pending");

        var response = ProductManagerAgent.EnsureAccurateApprovalStatus(
            "I submitted the complete team.",
            ResourceChangeApprovalToolResult.Success(request));

        Assert.Contains(request.Id.ToString("D"), response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Pending", response, StringComparison.Ordinal);
    }

    [Fact]
    public void ContextUpdate_WaitsForGaps_AndRefreshesWhenReady()
    {
        var gapBrief = new ProductRoleBriefResponse(
            "AwaitingExecutiveInput", Guid.NewGuid(), Guid.NewGuid(), 1, "Pending",
            [], [], [], [],
            [], [new ProductRoleBriefGap("customer", "Who is the customer?", "Changes product scope.")],
            DateTimeOffset.UtcNow);
        var waiting = ProductManagerOrchestrator.BuildContextUpdateResponse(
            new ProductContextUpdateRequest(gapBrief, Guid.NewGuid(), "update-1"));
        var ready = ProductManagerOrchestrator.BuildContextUpdateResponse(
            new ProductContextUpdateRequest(gapBrief with
            {
                Status = "Ready",
                MissingInformation = []
            }, Guid.NewGuid(), "update-2"));

        Assert.Equal("Waiting", waiting.State);
        Assert.False(waiting.PlanRefreshRequired);
        Assert.Equal("Ready", ready.State);
        Assert.True(ready.PlanRefreshRequired);
    }

    [Fact]
    public void ManagementReport_IsProductFocusedAndConcise()
    {
        var organization = new OrganizationSnapshotResponse(
            Guid.NewGuid(), "Active", [], [], [],
            [new WorkstreamSummary(Guid.NewGuid(), "Launch", "Ship a validated release", "Blocked", "Launch", null,
                DateTimeOffset.UtcNow.AddDays(-1), null, null)],
            [], DateTimeOffset.UtcNow);
        var context = new ProductOperatingContext(null, null, organization, null, null, null, []);
        var request = new ManagementCheckInRequest(
            Guid.NewGuid(), "ManagerRollup", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow,
            [], [], DateTimeOffset.UtcNow.AddHours(2));

        var report = ProductManagerOrchestrator.BuildManagementReport(request, context);

        Assert.Contains("product", report.Markdown!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Launch", report.Blockers);
        Assert.True(report.ImmediateActions.Count <= 5);
        Assert.True(report.ConversationTopics.Count <= 3);
    }

    private static string ManifestPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "csweet-plugin.json");
            if (File.Exists(candidate) &&
                File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("csweet-plugin.json was not found.");
    }

    private static ResourceChangeRole Role(string key, string title, int priority, string timing) =>
        new(
            key,
            "Product",
            title,
            $"Own {title}.",
            1,
            priority,
            timing,
            ["product-delivery"],
            false,
            Guid.NewGuid(),
            null);

    private static ResourceChangeRequestResponse ResourceChange(
        Guid requestId,
        Guid organizationId,
        Guid conversationId,
        string goal,
        string status)
    {
        var requester = Guid.NewGuid();
        var role = Role("engineer", "Product Engineer", 1, "Now") with
        {
            ReportsToOrganizationUserId = requester
        };
        return new ResourceChangeRequestResponse(
            requestId,
            organizationId,
            requester,
            Guid.NewGuid(),
            Guid.NewGuid(),
            conversationId,
            Guid.Empty,
            goal,
            "Smallest complete team.",
            1,
            [role],
            [new ResourceChangeRoleDelta("Add", role, null)],
            [],
            [],
            null,
            status,
            "Delivered",
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private static AgentEventEnvelope RecommendationFulfilled(Guid organizationId, Guid sourceRequestId)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        return new AgentEventEnvelope(
            Guid.NewGuid(),
            Guid.NewGuid(),
            HiringEvents.RecommendationFulfilled,
            JsonSerializer.SerializeToElement(new HiringRecommendationFulfilledEvent(
                organizationId,
                Guid.NewGuid(),
                sourceRequestId,
                Guid.NewGuid(),
                "product-engineer",
                "Product Engineer",
                Guid.NewGuid(),
                null,
                1,
                1,
                [Guid.NewGuid()],
                occurredAt)),
            occurredAt);
    }

    private sealed class TestLlmClientFactory(IChatClient chatClient) : IAgentLlmClientFactory
    {
        public Task<IChatClient> CreateChatClientAsync(
            AgentLlmSelection selection,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(chatClient);
    }

    private sealed class CapturingChatClient(string response) : IChatClient
    {
        public string Prompt { get; private set; } = string.Empty;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Prompt = string.Join("\n", messages.Select(message => message.Text));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Prompt = string.Join("\n", messages.Select(message => message.Text));
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, response);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
