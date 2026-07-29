using System.Text.Json;
using CSweet.Agent.SDK;

namespace CSweet.Agents.ProductManager.Tests;

public sealed class ProductManagerProfileTests
{
    [Fact]
    public void Manifest_UsesProductIdentityAndLeastPrivilegeCoordination()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath()));
        var root = document.RootElement;
        Assert.Equal(ProductManagerProfile.AgentId, root.GetProperty("id").GetString());
        Assert.Equal(ProductManagerProfile.Version, root.GetProperty("version").GetString());
        var provides = root.GetProperty("provides").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).ToHashSet();
        var requires = root.GetProperty("requires").EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()).ToHashSet();
        Assert.All(provides.Concat(requires), capability =>
            Assert.Contains(capability!, CapabilityCatalog.All));
        Assert.Contains(ProductManagementCapabilities.Plan, provides);
        Assert.Contains(ProductManagementCapabilities.ContextUpdate, provides);
        Assert.Contains(ProductManagementCapabilities.RoleBrief, requires);
        Assert.Contains(ProductManagementCapabilities.PlanReview, requires);
        Assert.Contains(ProductManagementCapabilities.Escalation, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringRecommendationList, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringRecommendationUpsert, requires);
        Assert.DoesNotContain(PlatformCapabilities.HiringWorkflowStage, requires);
        Assert.Contains(ProductManagerProfile.CreateCommunicationCapability, requires);
        Assert.Contains(ProductManagerProfile.SendCommunicationMessageCapability, requires);
        Assert.Contains(AgentLifecycleCapabilities.CompleteOnboarding, requires);
    }

    [Fact]
    public void SystemPrompt_EnforcesProductAndChiefBoundaries()
    {
        Assert.Contains("customer discovery", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("roadmap", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("success measures", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("at most two", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("directly message your managing employee", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CEO, Chief of Staff, another human, or another agent", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never maintain the Chief's hiring backlog", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not present a finalized role list", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("routes the request to your authoritative manager", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not provide technical architecture", ProductManagerProfile.SystemPrompt, StringComparison.OrdinalIgnoreCase);
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
        Assert.All(plan.TeamStructure, role => Assert.Equal(ProductManagerProfile.DefaultDisplayName, role.ReportsTo));
        Assert.NotEmpty(plan.HiringSequence);
        Assert.NotEmpty(plan.Assumptions);
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
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("csweet-plugin.json was not found.");
    }
}
