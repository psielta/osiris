using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Api.IntegrationTests.Support;

namespace Osiris.Api.IntegrationTests.FinancialAccounts;

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class PdfImportFlowTests : IAsyncLifetime
{
    private const int Checking = 1;
    private const int Income = 1;
    private const int Expense = 2;

    private readonly OsirisApiApplicationFactory _factory;

    public PdfImportFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Preview_returns_extracted_transactions()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Banco", initialBalance: 200m);

        var response = await PostPreviewAsync(client, accountId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var preview = (await response.Content.ReadFromJsonAsync<OfxPreviewResponse>())!;
        Assert.Equal(2, preview.TotalCount);
        Assert.Equal(2, preview.NewCount);
    }

    [Fact]
    public async Task Import_persists_movements_and_skips_on_reimport()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Banco", initialBalance: 200m);

        var first = await PostImportAsync(client, accountId);
        var firstResult = (await first.Content.ReadFromJsonAsync<OfxResultResponse>())!;
        Assert.Equal(2, firstResult.Imported);
        Assert.Equal(1610m, await StatementBalanceAsync(client, accountId)); // 200 + 1500 - 90

        var second = await PostImportAsync(client, accountId);
        var secondResult = (await second.Content.ReadFromJsonAsync<OfxResultResponse>())!;
        Assert.Equal(0, secondResult.Imported);
        Assert.Equal(2, secondResult.SkippedDuplicates);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "pdf-owner@osiris.test")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var tokens = await ApiTestHelpers.RegisterAsync(client, email: email);
        ApiTestHelpers.Authorize(client, tokens.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string name, int type = Checking, decimal initialBalance = 0m)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new { name, type, initialBalance });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<HttpResponseMessage> PostPreviewAsync(HttpClient client, Guid accountId)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 fake"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "extrato.pdf");

        return await client.PostAsync($"/api/v1/accounts/{accountId}/movements/import/pdf/preview", content);
    }

    private static Task<HttpResponseMessage> PostImportAsync(HttpClient client, Guid accountId) =>
        client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/movements/import", new
        {
            lines = new[]
            {
                new { externalId = "PDF-A1", occurredOn = "2026-02-01", amount = 1500m, type = Income, description = "Salario pdf", categoryId = (Guid?)null },
                new { externalId = "PDF-A2", occurredOn = "2026-02-02", amount = 90m, type = Expense, description = "Mercado pdf", categoryId = (Guid?)null },
            },
        });

    private static async Task<decimal> StatementBalanceAsync(HttpClient client, Guid accountId)
    {
        var statement = await client.GetFromJsonAsync<StatementResponse>($"/api/v1/accounts/{accountId}/statement");
        return statement!.CurrentBalance;
    }
}
