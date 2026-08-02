using CSweet.Agent.SDK;

namespace CSweet.Agent.SoftwareProductManager.Tests;

public sealed class SdkRuntimeAbstractionTests
{
    [Fact]
    public async Task InMemoryRuntime_InvokesGrantedCapabilityAndRecordsProgress()
    {
        var runtime = new AgentTestRuntime()
            .RegisterCapability<EchoRequest, EchoResponse>(
                "test.echo.v1",
                (request, _) => Task.FromResult(new EchoResponse(request.Value)));
        var context = runtime.CreateContext();

        var response = await context.Platform.InvokeAsync<EchoRequest, EchoResponse>(
            "test.echo.v1",
            new EchoRequest("ready"));
        await context.ReportProgressAsync(new { phase = "verified" });

        Assert.Equal("ready", response.Value);
        Assert.Equal("verified", Assert.Single(runtime.Progress).GetProperty("phase").GetString());
    }

    private sealed record EchoRequest(string Value);
    private sealed record EchoResponse(string Value);
}
