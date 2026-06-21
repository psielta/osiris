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
public sealed class OfxReconciliationFlowTests : IAsyncLifetime
{
    // WEB-R1 (1500 on 02/06) matches the seeded manual movement; WEB-R2 has no match.
    private const string Ofx = """
        <OFX>
        <BANKTRANLIST>
        <STMTTRN><TRNTYPE>CREDIT<DTPOSTED>20260602<TRNAMT>1500.00<FITID>WEB-R1<MEMO>Salario web</STMTTRN>
        <STMTTRN><TRNTYPE>DEBIT<DTPOSTED>20260605<TRNAMT>-90.00<FITID>WEB-R2<MEMO>Mercado web</STMTTRN>
        </BANKTRANLIST>
        </OFX>
        """;

    private readonly OsirisWebApplicationFactory _factory;

    public OfxReconciliationFlowTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Preview_ShouldSuggestReconciliation_ForMatchingManualMovement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var account = await CreateAccountAsync(client);
        await CreateManualMovementAsync(client, account.Id, FinancialAccountMovementType.Income, "1500.00", "2026-06-02", "Salario");

        var response = await PostPreviewAsync(client, account.Id);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Conciliar com existente", html);
        Assert.Contains("Sugestão de conciliação", html);
        Assert.Contains("Mercado web", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Confirm_WithReconcile_ShouldLinkExisting_NotCreateNew_AndKeepBalance()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var account = await CreateAccountAsync(client); // initial 200
        await CreateManualMovementAsync(client, account.Id, FinancialAccountMovementType.Income, "1500.00", "2026-06-02", "Salario");

        var manualId = (await FindManualMovementAsync())!.Id;
        Assert.Equal(1, await MovementCountAsync());
        Assert.Equal(1700.00m, (await FindAccountAsync(account.Id))!.CurrentBalance); // 200 + 1500

        var confirm = await PostConfirmAsync(client, account.Id, reconcileWithMovementId: manualId);
        Assert.Equal(HttpStatusCode.Redirect, confirm.StatusCode);

        // Only the unmatched WEB-R2 created a new row; WEB-R1 was reconciled into the manual movement.
        Assert.Equal(2, await MovementCountAsync());
        var manual = await FindMovementByIdAsync(manualId);
        Assert.Equal("WEB-R1", manual!.ExternalId);
        Assert.NotNull(manual.ReconciledAtUtc);

        // Balance: reconcile did not re-apply +1500; only the new -90 moved it. 1700 - 90 = 1610.
        Assert.Equal(1610.00m, (await FindAccountAsync(account.Id))!.CurrentBalance);

        // Re-importing now shows WEB-R1 as already imported (it carries the external id).
        var reimport = await PostPreviewAsync(client, account.Id);
        Assert.Contains("Já importado", await reimport.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Confirm_WhenUserChoosesNewDespiteSuggestion_ShouldCreateNewMovement()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);
        var account = await CreateAccountAsync(client);
        await CreateManualMovementAsync(client, account.Id, FinancialAccountMovementType.Income, "1500.00", "2026-06-02", "Salario");
        var manualId = (await FindManualMovementAsync())!.Id;

        var confirm = await PostConfirmAsync(client, account.Id, reconcileWithMovementId: null);
        Assert.Equal(HttpStatusCode.Redirect, confirm.StatusCode);

        // Both lines imported as new -> manual + WEB-R1 + WEB-R2 = 3; the manual stays unlinked.
        Assert.Equal(3, await MovementCountAsync());
        Assert.Null((await FindMovementByIdAsync(manualId))!.ExternalId);
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

    private async Task CreateManualMovementAsync(
        HttpClient client,
        Guid accountId,
        FinancialAccountMovementType type,
        string amount,
        string occurredOn,
        string description)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}");
        var response = await client.PostAsync($"/accounts/{accountId}/movements", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Movement.Type"] = ((int)type).ToString(),
            ["Movement.Amount"] = amount,
            ["Movement.OccurredOn"] = occurredOn,
            ["Movement.Description"] = description,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostPreviewAsync(HttpClient client, Guid accountId)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}/import");

        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(Ofx));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", "extrato.ofx");
        content.Add(new StringContent(token), "__RequestVerificationToken");

        return await client.PostAsync($"/accounts/{accountId}/import/preview", content);
    }

    private static async Task<HttpResponseMessage> PostConfirmAsync(HttpClient client, Guid accountId, Guid? reconcileWithMovementId)
    {
        var token = await IntegrationTestHelpers.GetAntiForgeryTokenAsync(client, $"/accounts/{accountId}/import");

        var fields = new Dictionary<string, string>
        {
            ["Lines[0].Action"] = reconcileWithMovementId is null ? "New" : "Reconcile",
            ["Lines[0].ExternalId"] = "WEB-R1",
            ["Lines[0].OccurredOn"] = "2026-06-02",
            ["Lines[0].Amount"] = "1500.00",
            ["Lines[0].Type"] = nameof(FinancialAccountMovementType.Income),
            ["Lines[0].Description"] = "Salario web",
            ["Lines[0].IsDuplicate"] = "false",
            ["Lines[1].Action"] = "New",
            ["Lines[1].ExternalId"] = "WEB-R2",
            ["Lines[1].OccurredOn"] = "2026-06-05",
            ["Lines[1].Amount"] = "90.00",
            ["Lines[1].Type"] = nameof(FinancialAccountMovementType.Expense),
            ["Lines[1].Description"] = "Mercado web",
            ["Lines[1].IsDuplicate"] = "false",
            ["__RequestVerificationToken"] = token
        };

        if (reconcileWithMovementId is { } id)
        {
            fields["Lines[0].ReconcileWithMovementId"] = id.ToString();
        }

        return await client.PostAsync($"/accounts/{accountId}/import/confirm", new FormUrlEncodedContent(fields));
    }

    private async Task<FinancialAccountMovement?> FindManualMovementAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccountMovements.SingleOrDefaultAsync(movement => movement.ExternalId == null);
    }

    private async Task<FinancialAccountMovement?> FindMovementByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.FinancialAccountMovements.SingleOrDefaultAsync(movement => movement.Id == id);
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
