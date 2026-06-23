using System.Text.Json;
using Osiris.Application.Common.AI;

namespace Osiris.Api.IntegrationTests.Support;

/// <summary>
/// Drives the write flow deterministically: lists accounts to learn an accountId, then calls
/// propose_manual_movement on it, then answers. Used only when the writes feature is enabled.
/// </summary>
public sealed class WriteProposingFakeAiModelClient : IAiModelClient
{
    public Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        string? accountId = null;
        var proposed = false;

        foreach (var message in request.Messages)
        {
            if (message.Role != AiModelRole.Tool)
            {
                continue;
            }

            foreach (var result in message.ToolResults)
            {
                if (result.Name == "propose_manual_movement")
                {
                    proposed = true;
                }
                else if (result.Name == "list_financial_accounts")
                {
                    accountId ??= FirstAccountId(result.ResultJson);
                }
            }
        }

        var proposeOffered = request.Tools.Any(tool => tool.Name == "propose_manual_movement");
        if (proposed || !proposeOffered)
        {
            return Final();
        }

        return accountId is not null
            ? Call("propose_manual_movement", $"{{\"accountId\":\"{accountId}\",\"type\":\"expense\",\"amount\":50,\"description\":\"Mercado\"}}")
            : Call("list_financial_accounts", "{}");
    }

    private static string? FirstAccountId(string resultJson)
    {
        try
        {
            using var document = JsonDocument.Parse(resultJson);
            var accounts = document.RootElement.GetProperty("accounts");
            return accounts.GetArrayLength() > 0 ? accounts[0].GetProperty("id").GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Task<AiModelTurnResult> Call(string toolName, string argumentsJson) =>
        Task.FromResult(new AiModelTurnResult(
            null,
            new[] { new AiModelToolCall(string.Empty, toolName, JsonDocument.Parse(argumentsJson).RootElement.Clone()) },
            new AiUsage(5, 0, 0),
            AiFinishReason.ToolCalls,
            "fake-writer"));

    private static Task<AiModelTurnResult> Final() =>
        Task.FromResult(new AiModelTurnResult(
            "Criei uma proposta. Confirme para registrar o lançamento.",
            Array.Empty<AiModelToolCall>(),
            new AiUsage(4, 6, 0),
            AiFinishReason.Stop,
            "fake-writer"));
}
