using System.Runtime.CompilerServices;
using System.Text.Json;
using CSweet.Agent.Contracts.Grpc;
using CSweet.Agent.SDK;
using Google.Protobuf;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CSweet.Agents.ProductManager.Tests;

public sealed class BrokerToolCallingTests
{
    [Fact]
    public async Task ChatClientAgent_InvokesBrokeredFunctionCallAndReturnsFinalText()
    {
        var broker = new ToolCallingBrokerClient();
        using var chatClient = new BrokerLlmClient(
            broker,
            new AgentLlmSelection(Guid.NewGuid(), "model"));
        string? askedQuestion = null;
        var askUser = AIFunctionFactory.Create(
            (string question) =>
            {
                askedQuestion = question;
                return "The owner selected Product Manager.";
            },
            "ask_user",
            "Ask the owner one question.");
        AIAgent agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Instructions = "Recommend roles only.",
                    Tools = [askUser]
                }
            });
        var session = await agent.CreateSessionAsync();
        var text = new List<string>();

        await foreach (var update in agent.RunStreamingAsync(
            "Help me staff the company.",
            session))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                text.Add(update.Text);
            }
        }

        Assert.Equal("Which role should we hire first?", askedQuestion);
        Assert.Equal("Product Manager is the first role to fill.", string.Concat(text));
        Assert.Equal(2, broker.Requests.Count);
        Assert.Contains(
            broker.Requests[1].Messages.SelectMany(message => message.Contents ?? []),
            content => content.Kind == "function_result" && content.CallId == "call-1");
    }

    private sealed class ToolCallingBrokerClient : IAgentBrokerClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        public List<BrokerLlmRequest> Requests { get; } = [];

        public Task StartAsync(RegisterAgent registration, CancellationToken cancellationToken) => Task.CompletedTask;

        public async IAsyncEnumerable<BrokerToAgentMessage> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task PublishEventAsync(PublishEvent message, string? correlationId = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CapabilityResult> InvokeCapabilityAsync(RequestCapability request, string? correlationId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<CapabilityResult> InvokeStreamingCapabilityAsync(
            RequestCapability request,
            string? correlationId = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var parsed = JsonSerializer.Deserialize<BrokerLlmRequest>(request.Payload.Span, JsonOptions)!;
            Requests.Add(parsed);
            var chunk = Requests.Count == 1
                ? new BrokerLlmChunk(
                    null,
                    Role: "assistant",
                    Contents:
                    [
                        new BrokerLlmContent(
                            "function_call",
                            CallId: "call-1",
                            Name: "ask_user",
                            Arguments: new Dictionary<string, JsonElement>
                            {
                                ["question"] = JsonSerializer.SerializeToElement("Which role should we hire first?")
                            })
                    ])
                : new BrokerLlmChunk("Product Manager is the first role to fill.");
            await Task.Yield();
            yield return new CapabilityResult
            {
                RequestId = request.RequestId,
                Succeeded = true,
                ContentType = "application/json",
                Payload = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(chunk, JsonOptions)),
                Sequence = 0,
                HasMore = false
            };
        }

        public Task SendCapabilityResultAsync(CapabilityResult result, string? correlationId = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
