using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Osiris.Application.Common.Exceptions;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Gemini;

namespace Osiris.Web.IntegrationTests.Gemini;

/// <summary>
/// Unit tests for the real Gemini client's request/response handling, using a stub
/// <see cref="HttpMessageHandler"/> (no network, no API key). Lives here to reuse the project's
/// transitive Infrastructure reference.
/// </summary>
public sealed class GeminiPdfStatementExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ParsesTransactions_FromGeminiJson()
    {
        var inner = """[{"date":"2026-02-01","description":"Salario","amount":1500.00,"type":"income"},{"date":"2026-02-02","description":"Mercado","amount":-90.00,"type":"expense"}]""";
        var extractor = ExtractorReturning(GeminiResponse(inner));

        var transactions = await extractor.ExtractAsync(Pdf(), CancellationToken.None);

        Assert.Equal(2, transactions.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), transactions[0].OccurredOn);
        Assert.Equal(FinancialAccountMovementType.Income, transactions[0].Type);
        Assert.Equal(1500.00m, transactions[0].Amount);
        Assert.Equal(FinancialAccountMovementType.Expense, transactions[1].Type);
        Assert.Equal(90.00m, transactions[1].Amount);
        Assert.StartsWith("pdf-syn:", transactions[0].ExternalId);
    }

    [Fact]
    public async Task ExtractAsync_StripsCodeFences_AndInfersTypeFromSign()
    {
        var inner = "```json\n[{\"date\":\"2026-03-10\",\"description\":\"Pix enviado\",\"amount\":-12.34,\"type\":\"\"}]\n```";
        var extractor = ExtractorReturning(GeminiResponse(inner));

        var transactions = await extractor.ExtractAsync(Pdf(), CancellationToken.None);

        var transaction = Assert.Single(transactions);
        Assert.Equal(FinancialAccountMovementType.Expense, transaction.Type);
        Assert.Equal(12.34m, transaction.Amount);
        Assert.Equal("Pix enviado", transaction.Description);
    }

    [Fact]
    public async Task ExtractAsync_WhenNonSuccessStatus_Throws()
    {
        var extractor = ExtractorReturning("rate limited", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<PdfStatementExtractionException>(
            () => extractor.ExtractAsync(Pdf(), CancellationToken.None));
    }

    [Fact]
    public async Task ExtractAsync_WhenApiKeyMissing_Throws()
    {
        var client = new HttpClient(new StubHandler("[]", HttpStatusCode.OK)) { BaseAddress = new Uri("https://example.test/") };
        var extractor = new GeminiPdfStatementExtractor(
            client,
            Options.Create(new GeminiOptions { ApiKey = string.Empty }),
            NullLogger<GeminiPdfStatementExtractor>.Instance);

        await Assert.ThrowsAsync<PdfStatementExtractionException>(
            () => extractor.ExtractAsync(Pdf(), CancellationToken.None));
    }

    private static byte[] Pdf() => Encoding.UTF8.GetBytes("%PDF-1.4 fake");

    // Wraps the model's array output as a Gemini response (parts[].text is a JSON string).
    private static string GeminiResponse(string innerJson)
    {
        var escaped = System.Text.Json.JsonSerializer.Serialize(innerJson);
        return "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":" + escaped + "}]}}]}";
    }

    private static GeminiPdfStatementExtractor ExtractorReturning(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var client = new HttpClient(new StubHandler(body, status)) { BaseAddress = new Uri("https://example.test/") };
        return new GeminiPdfStatementExtractor(
            client,
            Options.Create(new GeminiOptions { ApiKey = "test-key", Model = "gemini-3.5-flash" }),
            NullLogger<GeminiPdfStatementExtractor>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public StubHandler(string body, HttpStatusCode status)
        {
            _body = body;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }
}
