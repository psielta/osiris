using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Api.IntegrationTests.Support;

namespace Osiris.Api.IntegrationTests.FinancialAccounts;

public sealed record AccountItemResponse(Guid Id, string Name, int Type, decimal CurrentBalance, bool IsActive);

public sealed record AccountEditResponse(Guid Id, string Name, int Type, decimal InitialBalance);

public sealed record MovementResponse(Guid Id, int Type, decimal Amount, bool IsInflow, DateOnly OccurredOn, string Description, Guid? CategoryId, string? Notes);

public sealed record StatementResponse(Guid Id, string Name, int Type, decimal InitialBalance, decimal CurrentBalance, bool IsActive, IReadOnlyList<MovementResponse> Movements);

public sealed record CreatedIdResponse(Guid Id);

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class FinancialAccountsFlowTests : IAsyncLifetime
{
    private const int Checking = 1;
    private const int Income = 1;
    private const int Expense = 2;
    private const int ExpenseCategory = 2;

    private readonly OsirisApiApplicationFactory _factory;

    public FinancialAccountsFlowTests(OsirisApiApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> AuthenticatedClientAsync(string email = "owner@osiris.test")
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

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new { name, type = ExpenseCategory, color = (string?)null });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;
    }

    private static async Task<List<AccountItemResponse>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<AccountItemResponse>>("/api/v1/accounts"))!;

    private static async Task<StatementResponse> StatementAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<StatementResponse>($"/api/v1/accounts/{id}/statement"))!;

    private static Task<HttpResponseMessage> PostMovementAsync(
        HttpClient client, Guid accountId, int type, decimal? amount, string description = "Lançamento", Guid? categoryId = null) =>
        client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/movements", new
        {
            type,
            amount,
            occurredOn = "2026-06-16",
            description,
            categoryId,
            notes = (string?)null,
        });

    [Fact]
    public async Task Crud_create_list_get_update_archive()
    {
        var client = await AuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Banco", type = Checking, initialBalance = 200.00m });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        // CurrentBalance starts equal to InitialBalance.
        Assert.Contains(await ListAsync(client), a => a.Id == id && a.Name == "Banco" && a.CurrentBalance == 200.00m && a.IsActive);

        var get = await client.GetFromJsonAsync<AccountEditResponse>($"/api/v1/accounts/{id}");
        Assert.Equal(200.00m, get!.InitialBalance);

        var update = await client.PutAsJsonAsync($"/api/v1/accounts/{id}", new { name = "Banco Principal", type = Checking });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        // Edit changes name/type, never the balance.
        Assert.Contains(await ListAsync(client), a => a.Id == id && a.Name == "Banco Principal" && a.CurrentBalance == 200.00m);

        var archive = await client.PostAsync($"/api/v1/accounts/{id}/archive", content: null);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        Assert.Contains(await ListAsync(client), a => a.Id == id && !a.IsActive);
    }

    [Fact]
    public async Task Create_withBlankName_returns400()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "", type = Checking, initialBalance = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_withDuplicateName_returns400()
    {
        var client = await AuthenticatedClientAsync();
        await CreateAccountAsync(client, "Carteira");
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "carteira", type = Checking, initialBalance = 0m });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_toDuplicateName_returns400()
    {
        var client = await AuthenticatedClientAsync();
        await CreateAccountAsync(client, "Conta A");
        var bId = await CreateAccountAsync(client, "Conta B");

        var response = await client.PutAsJsonAsync($"/api/v1/accounts/{bId}", new { name = "Conta A", type = Checking });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mutations_onUnknownId_return404()
    {
        var client = await AuthenticatedClientAsync();
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/accounts/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/accounts/{unknown}/statement")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/accounts/{unknown}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"/api/v1/accounts/{unknown}", new { name = "X", type = Checking })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/accounts/{unknown}/archive", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostMovementAsync(client, unknown, Income, 10m)).StatusCode);
    }

    [Fact]
    public async Task Accounts_areIsolatedPerTenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var id = await CreateAccountAsync(clientA, "Conta de A", Checking, 100m);

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/accounts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/accounts/{id}/statement")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/accounts/{id}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await clientB.PutAsJsonAsync($"/api/v1/accounts/{id}", new { name = "Invadida", type = Checking })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsync($"/api/v1/accounts/{id}/archive", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostMovementAsync(clientB, id, Income, 10m)).StatusCode);
        Assert.DoesNotContain(await ListAsync(clientB), a => a.Id == id);
    }

    [Fact]
    public async Task Statement_pdf_returns_file_for_own_account()
    {
        var client = await AuthenticatedClientAsync();
        var id = await CreateAccountAsync(client, "Caixa", Checking, 200m);
        Assert.Equal(HttpStatusCode.Created, (await PostMovementAsync(client, id, Income, 50m, "Deposito")).StatusCode);

        var response = await client.GetAsync($"/api/v1/accounts/{id}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 100);
    }

    [Fact]
    public async Task Movement_income_increases_and_expense_decreases_balance()
    {
        var client = await AuthenticatedClientAsync();
        var id = await CreateAccountAsync(client, "Caixa", Checking, 200m);

        Assert.Equal(HttpStatusCode.Created, (await PostMovementAsync(client, id, Income, 50m, "Depósito")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostMovementAsync(client, id, Expense, 30m, "Saque")).StatusCode);

        var statement = await StatementAsync(client, id);
        Assert.Equal(200m, statement.InitialBalance);
        Assert.Equal(220m, statement.CurrentBalance); // 200 + 50 - 30
        Assert.Equal(2, statement.Movements.Count);
        Assert.Contains(statement.Movements, m => m.Description == "Depósito" && m.IsInflow && m.Amount == 50m);
        Assert.Contains(statement.Movements, m => m.Description == "Saque" && !m.IsInflow && m.Amount == 30m);
    }

    [Fact]
    public async Task Movement_withoutAmount_returns400_andDoesNotChangeBalance()
    {
        var client = await AuthenticatedClientAsync();
        var id = await CreateAccountAsync(client, "Conta", Checking, 200m);

        var response = await PostMovementAsync(client, id, Income, amount: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Equal(200m, (await StatementAsync(client, id)).CurrentBalance);
    }

    [Fact]
    public async Task Movement_onArchivedAccount_returns400()
    {
        var client = await AuthenticatedClientAsync();
        var id = await CreateAccountAsync(client, "Arquivada", Checking, 100m);
        await client.PostAsync($"/api/v1/accounts/{id}/archive", content: null);

        var response = await PostMovementAsync(client, id, Income, 10m);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Movement_withActiveCategory_succeeds_andStatementKeepsIt()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Conta", Checking, 0m);
        // "Padaria" is not one of the default seeded category names.
        var categoryId = await CreateCategoryAsync(client, "Padaria");

        var response = await PostMovementAsync(client, accountId, Expense, 25m, "Compras", categoryId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Assert.Contains((await StatementAsync(client, accountId)).Movements, m => m.CategoryId == categoryId);
    }

    [Fact]
    public async Task Movement_withArchivedCategory_returns400()
    {
        var client = await AuthenticatedClientAsync();
        var accountId = await CreateAccountAsync(client, "Conta", Checking, 0m);
        var categoryId = await CreateCategoryAsync(client, "Some Category");
        await client.PostAsync($"/api/v1/categories/{categoryId}/archive", content: null);

        var response = await PostMovementAsync(client, accountId, Expense, 25m, "Compras", categoryId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Movement_withCategoryFromAnotherTenant_returns400()
    {
        var clientB = await AuthenticatedClientAsync("bob@osiris.test");
        var foreignCategoryId = await CreateCategoryAsync(clientB, "Categoria do B");

        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var accountId = await CreateAccountAsync(clientA, "Conta de A", Checking, 0m);

        var response = await PostMovementAsync(clientA, accountId, Expense, 25m, "Compras", foreignCategoryId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
