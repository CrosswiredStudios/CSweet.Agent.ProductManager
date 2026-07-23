using System.Runtime.CompilerServices;
using System.Text.Json;
using CSweet.Agent.Contracts.Grpc;
using CSweet.Agent.SDK;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CSweet.Agents.ProductManager.Tests;

public sealed class ProductManagerProgressTests
{
    [Fact]
    public async Task UserMessage_PublishesTurnBoundProgressBeforePlatformReadsOrModelCreation()
    {
        var broker = new BlockingProgressBroker();
        var factory = new CountingLlmClientFactory();
        var agent = new ProductManagerAgent(
            factory,
            NullLogger<ProductManagerAgent>.Instance,
            new ProductManagerOrchestrator(NullLogger<ProductManagerOrchestrator>.Instance));
        var turnId = Guid.NewGuid();
        const int attempt = 3;
        var incoming = new UserMessageReceived(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            "Help me structure the initial development team.",
            null,
            turnId,
            attempt,
            Guid.NewGuid());
        var delivered = new DeliveredEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = ProductManagerProfile.UserMessageReceivedEvent,
            Payload = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(
                incoming,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))),
            OccurredAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var handling = agent.HandleEventAsync(
            delivered,
            new AgentRuntimeContext(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), broker),
            cancellation.Token);

        var progress = await broker.ProgressPublished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("progress", progress.Kind);
        Assert.Equal("accepted", progress.Metadata?["stage"]);
        Assert.Equal(0, progress.Sequence);
        Assert.Equal(turnId, progress.TurnId);
        Assert.Equal(attempt, progress.Attempt);
        Assert.Equal(0, broker.CapabilityInvocationCount);
        Assert.Equal(0, factory.CreationCount);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handling);
    }

    private sealed class CountingLlmClientFactory : IAgentLlmClientFactory
    {
        public int CreationCount { get; private set; }

        public Task<IChatClient> CreateChatClientAsync(
            AgentLlmSelection selection,
            CancellationToken cancellationToken = default)
        {
            CreationCount++;
            throw new InvalidOperationException("The model must not be created before progress is published.");
        }
    }

    private sealed class BlockingProgressBroker : IAgentBrokerClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public TaskCompletionSource<AssistantResponseChunk> ProgressPublished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CapabilityInvocationCount { get; private set; }

        public Task StartAsync(RegisterAgent registration, CancellationToken cancellationToken) => Task.CompletedTask;

        public async IAsyncEnumerable<BrokerToAgentMessage> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async Task PublishEventAsync(
            PublishEvent message,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            if (message.EventType != ProductManagerProfile.AssistantResponseChunkEvent) return;
            var chunk = JsonSerializer.Deserialize<AssistantResponseChunk>(message.Payload.Span, JsonOptions)!;
            if (!string.Equals(chunk.Kind, "progress", StringComparison.Ordinal)) return;
            ProgressPublished.TrySetResult(chunk);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public Task<CapabilityResult> InvokeCapabilityAsync(
            RequestCapability request,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            CapabilityInvocationCount++;
            throw new InvalidOperationException("Platform reads must not begin before progress is published.");
        }

        public async IAsyncEnumerable<CapabilityResult> InvokeStreamingCapabilityAsync(
            RequestCapability request,
            string? correlationId = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task SendCapabilityResultAsync(
            CapabilityResult result,
            string? correlationId = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
