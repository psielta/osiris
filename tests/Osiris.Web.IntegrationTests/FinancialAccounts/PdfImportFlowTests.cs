using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;
using Osiris.Infrastructure.Persistence;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.FinancialAccounts;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class PdfImportFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public PdfImportFlowTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Preview_ShouldListExtractedTransactions()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var account = await CreateAccountAsync(client);

        var response = await PostPreviewAsync(client, account.Id);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Salario pdf", html);
        Assert.Contains("Mercado pdf", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Confirm_ShouldPersistMovements_UpdateBalance_AndSkipOnReimport()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var account = await CreateAccountAsync(client);

        var confirm = await PostConfirmAsync(client, account.Id);
        Assert.Equal(HttpStatusCode.Redirect, confirm.StatusCode);

        Assert.Equal(2, await MovementCountAsync());
        Assert.Equal(1610.00m, (await FindAccountAsync(account.Id))!.CurrentBalance);

        var reimport = await PostConfirmAsync(client, account.Id);
        Assert.Equal(HttpStatusCode.Redirect, reimport.StatusCode);
        Assert.Equal(2, await MovementCountAsync());
    }

    private async Task<FinancialAccount> CreateAccountAsync(HttpClient client)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, "/accounts/create");
        var response = await client.PostAsync("/accounts/create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Banco",
            ["Type"] = ((int)FinancialAccountType.CheckingAccount).ToString(),
            ["InitialBalance"] = "200.00",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccounts.SingleAsync();
    }

    private static async Task<HttpResponseMessage> PostPreviewAsync(HttpClient client, Guid accountId)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}/import/pdf");

        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 fake"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "extrato.pdf");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        return await client.PostAsync($"/accounts/{accountId}/import/pdf/preview", content);
    }

    private static async Task<HttpResponseMessage> PostConfirmAsync(HttpClient client, Guid accountId)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}/import/pdf");

        return await client.PostAsync($"/accounts/{accountId}/import/confirm", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Lines[0].Action"] = "New",
            ["Lines[0].ExternalId"] = "PDF-A1",
            ["Lines[0].OccurredOn"] = "2026-02-01",
            ["Lines[0].Amount"] = "1500.00",
            ["Lines[0].Type"] = nameof(FinancialAccountMovementType.Income),
            ["Lines[0].Description"] = "Salario pdf",
            ["Lines[0].IsDuplicate"] = "false",
            ["Lines[1].Action"] = "New",
            ["Lines[1].ExternalId"] = "PDF-A2",
            ["Lines[1].OccurredOn"] = "2026-02-02",
            ["Lines[1].Amount"] = "90.00",
            ["Lines[1].Type"] = nameof(FinancialAccountMovementType.Expense),
            ["Lines[1].Description"] = "Mercado pdf",
            ["Lines[1].IsDuplicate"] = "false",
            ["__RequestVerificationToken"] = token
        }));
    }

    private async Task<FinancialAccount?> FindAccountAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccounts.SingleOrDefaultAsync(account => account.Id == id);
    }

    private async Task<int> MovementCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccountMovements.CountAsync();
    }
}
