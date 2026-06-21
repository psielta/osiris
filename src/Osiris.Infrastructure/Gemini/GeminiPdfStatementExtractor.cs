using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.Exceptions;
using Osiris.Application.Common.Pdf;
using Osiris.Domain.Enums;

namespace Osiris.Infrastructure.Gemini;

/// <summary>
/// Extracts statement transactions from a PDF using the Google Gemini REST API (generateContent with
/// inline PDF data and a forced JSON response schema). The first outbound HTTP client in the backend.
/// </summary>
public sealed class GeminiPdfStatementExtractor : IPdfStatementExtractor
{
    private const int MaxDescriptionLength = 200;
    private const int MaxExternalIdLength = 255;
    private const string FallbackDescription = "Lançamento importado";

    private const string Prompt =
        "Você é um extrator de extratos bancários. Extraia TODOS os lançamentos (transações) deste " +
        "extrato bancário em PDF. Para cada lançamento retorne: 'date' (data no formato ISO yyyy-MM-dd), " +
        "'description' (histórico/descrição do lançamento), 'amount' (valor numérico COM SINAL: negativo " +
        "para saídas/débitos/despesas e positivo para entradas/créditos/receitas) e 'type' ('expense' " +
        "para saídas, 'income' para entradas). Ignore tudo que não for um lançamento: saldo, saldo " +
        "anterior, totais, cabeçalhos e rodapés. Não invente lançamentos. Responda apenas com o array JSON.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly object ResponseSchema = new
    {
        type = "array",
        items = new
        {
            type = "object",
            properties = new
            {
                date = new { type = "string" },
                description = new { type = "string" },
                amount = new { type = "number" },
                type = new { type = "string", @enum = new[] { "income", "expense" } },
            },
            required = new[] { "date", "description", "amount", "type" },
        },
    };

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "dd/MM/yy", "MM/dd/yyyy"
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiPdfStatementExtractor> _logger;

    public GeminiPdfStatementExtractor(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiPdfStatementExtractor> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExtractedStatementTransaction>> ExtractAsync(
        byte[] content,
        CancellationToken cancellationToken)
    {
        if (content is null || content.Length == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new PdfStatementExtractionException("A chave da API do Gemini não está configurada.");
        }

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inlineData = new { mimeType = "application/pdf", data = Convert.ToBase64String(content) } },
                        new { text = Prompt },
                    },
                },
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = ResponseSchema,
            },
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{_options.Model}:generateContent");
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Content = JsonContent.Create(requestBody, options: SerializerOptions);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Gemini request failed (model {Model}).", _options.Model);
            throw new PdfStatementExtractionException("Falha ao chamar a API do Gemini.", exception);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Gemini returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                Truncate(responseBody, 500));
            throw new PdfStatementExtractionException($"A API do Gemini retornou {(int)response.StatusCode}.");
        }

        var payload = ExtractJsonPayload(responseBody);
        return ParseTransactions(payload);
    }

    private string ExtractJsonPayload(string responseBody)
    {
        GeminiResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<GeminiResponse>(responseBody, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new PdfStatementExtractionException("Resposta da IA em formato inesperado.", exception);
        }

        var parts = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts;
        if (parts is null || parts.Count == 0)
        {
            throw new PdfStatementExtractionException("A IA não retornou conteúdo para o PDF.");
        }

        var text = string.Concat(parts.Select(part => part.Text)).Trim();
        return StripCodeFences(text);
    }

    private IReadOnlyList<ExtractedStatementTransaction> ParseTransactions(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        List<GeminiTransaction>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<GeminiTransaction>>(payload, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new PdfStatementExtractionException(
                "Não foi possível interpretar os lançamentos retornados pela IA.",
                exception);
        }

        if (items is null)
        {
            return [];
        }

        var transactions = new List<ExtractedStatementTransaction>(items.Count);
        foreach (var item in items)
        {
            var occurredOn = ParseDate(item.Date);
            if (occurredOn is null || item.Amount is null || item.Amount.Value == 0m)
            {
                continue;
            }

            var signedAmount = item.Amount.Value;
            var type = ResolveType(item.Type, signedAmount);
            var description = BuildDescription(item.Description);
            var externalId = SynthesizeExternalId(occurredOn.Value, signedAmount, description);

            transactions.Add(new ExtractedStatementTransaction(
                externalId,
                occurredOn.Value,
                Math.Abs(signedAmount),
                type,
                description));
        }

        return transactions;
    }

    private static FinancialAccountMovementType ResolveType(string? type, decimal signedAmount)
    {
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalized = type.Trim().ToLowerInvariant();
            if (normalized.StartsWith("exp", StringComparison.Ordinal)
                || normalized.StartsWith("desp", StringComparison.Ordinal)
                || normalized.StartsWith("deb", StringComparison.Ordinal)
                || normalized.StartsWith("déb", StringComparison.Ordinal)
                || normalized.StartsWith("sai", StringComparison.Ordinal)
                || normalized.StartsWith("saí", StringComparison.Ordinal))
            {
                return FinancialAccountMovementType.Expense;
            }

            if (normalized.StartsWith("inc", StringComparison.Ordinal)
                || normalized.StartsWith("rec", StringComparison.Ordinal)
                || normalized.StartsWith("cred", StringComparison.Ordinal)
                || normalized.StartsWith("créd", StringComparison.Ordinal)
                || normalized.StartsWith("ent", StringComparison.Ordinal))
            {
                return FinancialAccountMovementType.Income;
            }
        }

        return signedAmount < 0m ? FinancialAccountMovementType.Expense : FinancialAccountMovementType.Income;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim();
        if (raw.Length > 10 && DateOnly.TryParse(raw[..10], CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return iso;
        }

        return DateOnly.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static string BuildDescription(string? description)
    {
        var collapsed = string.IsNullOrWhiteSpace(description)
            ? FallbackDescription
            : string.Join(' ', description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length == 0)
        {
            collapsed = FallbackDescription;
        }

        return collapsed.Length > MaxDescriptionLength ? collapsed[..MaxDescriptionLength] : collapsed;
    }

    private static string SynthesizeExternalId(DateOnly occurredOn, decimal signedAmount, string description)
    {
        var seed = $"{occurredOn:yyyyMMdd}|{signedAmount.ToString(CultureInfo.InvariantCulture)}|{description}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
        var externalId = $"pdf-syn:{hash[..32]}";
        return externalId.Length > MaxExternalIdLength ? externalId[..MaxExternalIdLength] : externalId;
    }

    private static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
        {
            return trimmed;
        }

        var body = trimmed[(firstNewline + 1)..];
        var closingFence = body.LastIndexOf("```", StringComparison.Ordinal);
        return (closingFence >= 0 ? body[..closingFence] : body).Trim();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record GeminiResponse(List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(GeminiContent? Content);

    private sealed record GeminiContent(List<GeminiPart>? Parts);

    private sealed record GeminiPart(string? Text);

    private sealed record GeminiTransaction(string? Date, string? Description, decimal? Amount, string? Type);
}
