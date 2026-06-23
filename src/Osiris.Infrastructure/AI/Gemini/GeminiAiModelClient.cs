using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.AI;
using Osiris.Application.Common.Exceptions;
using Osiris.Infrastructure.Gemini;

namespace Osiris.Infrastructure.AI.Gemini;

/// <summary>
/// <see cref="IAiModelClient"/> implemented over the Gemini generateContent REST API with function
/// calling, reusing the proven HttpClient approach already used for PDF extraction. This is the only
/// place that knows about Gemini's wire format; the orchestrator stays provider-neutral.
/// </summary>
public sealed class GeminiAiModelClient : IAiModelClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonElement EmptyArguments = JsonDocument.Parse("{}").RootElement.Clone();

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAiModelClient> _logger;

    public GeminiAiModelClient(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAiModelClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiModelTurnResult> GenerateAsync(AiModelRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AiModelException("O assistente não está configurado no momento.");
        }

        var model = ResolveModel(request.Purpose);
        var body = BuildBody(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{model}:generateContent")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Gemini agent request failed (model {Model}).", model);
            throw new AiModelException("O assistente está temporariamente indisponível.", exception);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Gemini agent returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                Truncate(responseBody, 500));
            throw new AiModelException("O assistente está temporariamente indisponível.");
        }

        return Parse(responseBody, model);
    }

    private string ResolveModel(AiModelPurpose purpose) =>
        purpose == AiModelPurpose.Fast ? _options.FastModel : _options.AgentModel;

    private JsonObject BuildBody(AiModelRequest request)
    {
        var body = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = request.SystemPrompt })
            },
            ["contents"] = BuildContents(request.Messages),
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = _options.Temperature,
                ["maxOutputTokens"] = _options.MaxOutputTokens
            }
        };

        if (request.Tools.Count > 0)
        {
            var declarations = new JsonArray();
            foreach (var tool in request.Tools)
            {
                declarations.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonSerializer.SerializeToNode(tool.ParametersSchema, SerializerOptions)
                });
            }

            body["tools"] = new JsonArray(new JsonObject { ["function_declarations"] = declarations });
        }

        return body;
    }

    private static JsonArray BuildContents(IReadOnlyList<AiModelMessage> messages)
    {
        var contents = new JsonArray();
        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case AiModelRole.User:
                    contents.Add(TextContent("user", message.Text));
                    break;

                case AiModelRole.Model when message.ToolCalls.Count > 0:
                    var callParts = new JsonArray();
                    foreach (var call in message.ToolCalls)
                    {
                        callParts.Add(new JsonObject
                        {
                            ["functionCall"] = new JsonObject
                            {
                                ["name"] = call.Name,
                                ["args"] = ElementToNode(call.Arguments)
                            }
                        });
                    }

                    contents.Add(new JsonObject { ["role"] = "model", ["parts"] = callParts });
                    break;

                case AiModelRole.Model:
                    contents.Add(TextContent("model", message.Text));
                    break;

                case AiModelRole.Tool:
                    var responseParts = new JsonArray();
                    foreach (var result in message.ToolResults)
                    {
                        responseParts.Add(new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = result.Name,
                                ["response"] = WrapToolResponse(result.ResultJson)
                            }
                        });
                    }

                    // Gemini carries function responses in a content turn (role "user").
                    contents.Add(new JsonObject { ["role"] = "user", ["parts"] = responseParts });
                    break;
            }
        }

        return contents;
    }

    private static JsonObject TextContent(string role, string? text) =>
        new() { ["role"] = role, ["parts"] = new JsonArray(new JsonObject { ["text"] = text ?? string.Empty }) };

    private static JsonNode ElementToNode(JsonElement element) =>
        element.ValueKind == JsonValueKind.Undefined
            ? new JsonObject()
            : JsonNode.Parse(element.GetRawText()) ?? new JsonObject();

    private static JsonObject WrapToolResponse(string resultJson)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(resultJson);
        }
        catch (JsonException)
        {
            parsed = resultJson;
        }

        return new JsonObject { ["result"] = parsed };
    }

    private AiModelTurnResult Parse(string responseBody, string model)
    {
        GeminiResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<GeminiResponse>(responseBody, SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Gemini agent returned an unparsable response.");
            throw new AiModelException("O assistente retornou uma resposta inesperada.", exception);
        }

        var usage = parsed?.UsageMetadata is { } metadata
            ? new AiUsage(metadata.PromptTokenCount, metadata.CandidatesTokenCount, metadata.CachedContentTokenCount ?? 0)
            : AiUsage.Empty;

        var candidate = parsed?.Candidates?.FirstOrDefault();
        var parts = candidate?.Content?.Parts;
        if (parts is null || parts.Count == 0)
        {
            var blockReason = parsed?.PromptFeedback?.BlockReason;
            if (!string.IsNullOrEmpty(blockReason))
            {
                _logger.LogWarning("Gemini agent blocked the prompt: {Reason}", blockReason);
                return new AiModelTurnResult(
                    "Não posso ajudar com esse pedido.",
                    Array.Empty<AiModelToolCall>(),
                    usage,
                    AiFinishReason.Safety,
                    model);
            }

            return new AiModelTurnResult(string.Empty, Array.Empty<AiModelToolCall>(), usage, AiFinishReason.Other, model);
        }

        var text = new StringBuilder();
        var toolCalls = new List<AiModelToolCall>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                text.Append(part.Text);
            }

            if (part.FunctionCall is { Name: { Length: > 0 } name })
            {
                var arguments = part.FunctionCall.Args is { } args && args.ValueKind != JsonValueKind.Undefined
                    ? args.Clone()
                    : EmptyArguments;
                toolCalls.Add(new AiModelToolCall(string.Empty, name, arguments));
            }
        }

        var finishReason = MapFinishReason(candidate?.FinishReason, toolCalls.Count > 0);
        return new AiModelTurnResult(
            text.Length > 0 ? text.ToString() : null,
            toolCalls,
            usage,
            finishReason,
            model);
    }

    private static AiFinishReason MapFinishReason(string? reason, bool hasToolCalls)
    {
        if (hasToolCalls)
        {
            return AiFinishReason.ToolCalls;
        }

        return reason?.ToUpperInvariant() switch
        {
            "STOP" => AiFinishReason.Stop,
            "MAX_TOKENS" => AiFinishReason.MaxTokens,
            "SAFETY" => AiFinishReason.Safety,
            _ => AiFinishReason.Other
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record GeminiResponse(
        List<GeminiCandidate>? Candidates,
        GeminiUsage? UsageMetadata,
        GeminiPromptFeedback? PromptFeedback);

    private sealed record GeminiCandidate(GeminiContent? Content, string? FinishReason);

    private sealed record GeminiContent(List<GeminiPart>? Parts, string? Role);

    private sealed record GeminiPart(string? Text, GeminiFunctionCall? FunctionCall);

    private sealed record GeminiFunctionCall(string? Name, JsonElement? Args);

    private sealed record GeminiUsage(int PromptTokenCount, int CandidatesTokenCount, int? CachedContentTokenCount);

    private sealed record GeminiPromptFeedback(string? BlockReason);
}
