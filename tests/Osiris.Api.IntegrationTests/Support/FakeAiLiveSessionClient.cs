using System.Runtime.CompilerServices;
using System.Text.Json;
using Osiris.Application.Common.AI;

namespace Osiris.Api.IntegrationTests.Support;

public sealed class FakeAiLiveSessionClient : IAiLiveSessionClient
{
    public List<AiLiveSessionRequest> Requests { get; } = new();
    public List<IReadOnlyList<AiModelToolResult>> ToolResults { get; } = new();

    public Task<IAiLiveSession> ConnectAsync(AiLiveSessionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult<IAiLiveSession>(new Session(this));
    }

    private sealed class Session : IAiLiveSession
    {
        private readonly FakeAiLiveSessionClient _owner;

        public Session(FakeAiLiveSessionClient owner)
        {
            _owner = owner;
        }

        public Task SendAudioAsync(ReadOnlyMemory<byte> pcm16, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SendTextAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SignalAudioEndAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SendToolResultsAsync(
            IReadOnlyList<AiModelToolResult> results,
            CancellationToken cancellationToken)
        {
            _owner.ToolResults.Add(results);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<AiLiveServerEvent> ReadEventsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new AiLiveTranscript("crie uma conta de internet de 10 reais", IsUser: true, Final: true);
            yield return new AiLiveToolCallEvent(new[]
            {
                new AiModelToolCall(
                    "call-1",
                    "propose_bill_creation",
                    JsonDocument.Parse(
                        """
                        {
                          "description": "Internet",
                          "amount": 10,
                          "dueDate": "2026-06-30",
                          "categoryId": "11111111-1111-1111-1111-111111111111"
                        }
                        """).RootElement.Clone())
            });
            yield return new AiLiveTranscript("Criei a proposta. Confirme na tela.", IsUser: false, Final: true);
            yield return new AiLiveTurnComplete();

            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
