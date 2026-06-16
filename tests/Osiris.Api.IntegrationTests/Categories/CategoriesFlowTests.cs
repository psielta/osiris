using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Osiris.Api.IntegrationTests.Support;
using Osiris.Domain.Entities;
using Osiris.Infrastructure.Persistence;

namespace Osiris.Api.IntegrationTests.Categories;

public sealed record CategoryItemResponse(Guid Id, string Name, int Type, string? Color, bool IsActive);

public sealed record CategoryEditResponse(Guid Id, string Name, int Type, string? Color);

public sealed record CreatedCategoryResponse(Guid Id);

[Collection(ApiIntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CategoriesFlowTests : IAsyncLifetime
{
    private const int Expense = 2;

    private readonly OsirisApiApplicationFactory _factory;

    public CategoriesFlowTests(OsirisApiApplicationFactory factory)
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

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name, int type = Expense, string? color = "#F59E0B")
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new { name, type, color });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedCategoryResponse>())!.Id;
    }

    private static async Task<List<CategoryItemResponse>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<CategoryItemResponse>>("/api/v1/categories"))!;

    [Fact]
    public async Task Crud_create_list_get_update_archive_delete()
    {
        var client = await AuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Aluguel", type = Expense, color = "#F59E0B" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedCategoryResponse>())!.Id;

        Assert.Contains(await ListAsync(client), c => c.Id == id && c.Name == "Aluguel" && c.Type == Expense && c.IsActive);

        var get = await client.GetAsync($"/api/v1/categories/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var edit = await get.Content.ReadFromJsonAsync<CategoryEditResponse>();
        Assert.Equal("Aluguel", edit!.Name);

        // "Aluguel Editado" avoids colliding with the default seeded category names (e.g. "Moradia").
        var update = await client.PutAsJsonAsync($"/api/v1/categories/{id}", new { name = "Aluguel Editado", type = Expense, color = "#6366F1" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Contains(await ListAsync(client), c => c.Id == id && c.Name == "Aluguel Editado" && c.Color == "#6366F1");

        var archive = await client.PostAsync($"/api/v1/categories/{id}/archive", content: null);
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        Assert.Contains(await ListAsync(client), c => c.Id == id && !c.IsActive);

        var delete = await client.DeleteAsync($"/api/v1/categories/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.DoesNotContain(await ListAsync(client), c => c.Id == id);
    }

    [Fact]
    public async Task Create_withBlankName_returns400()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/categories", new { name = "", type = Expense, color = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_withInvalidColor_returns400()
    {
        var client = await AuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Cor ruim", type = Expense, color = "vermelho" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mutations_onUnknownId_return404()
    {
        var client = await AuthenticatedClientAsync();
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/categories/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"/api/v1/categories/{unknown}", new { name = "X", type = Expense, color = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/categories/{unknown}/archive", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/categories/{unknown}")).StatusCode);
    }

    [Fact]
    public async Task Categories_areIsolatedPerTenant()
    {
        var clientA = await AuthenticatedClientAsync("alice@osiris.test");
        var id = await CreateCategoryAsync(clientA, "Privada de A");

        var clientB = await AuthenticatedClientAsync("bob@osiris.test");

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/v1/categories/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await clientB.PutAsJsonAsync($"/api/v1/categories/{id}", new { name = "Invadida", type = Expense, color = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.DeleteAsync($"/api/v1/categories/{id}")).StatusCode);
        Assert.DoesNotContain(await ListAsync(clientB), c => c.Id == id);
    }

    [Fact]
    public async Task Delete_whenReferencedByBill_returns409()
    {
        var client = await AuthenticatedClientAsync("bills@osiris.test");
        var id = await CreateCategoryAsync(client, "Aluguel");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenantId = await dbContext.Tenants.Select(tenant => tenant.Id).SingleAsync();
            dbContext.Bills.Add(new Bill(tenantId, id, "Aluguel de julho", 1200m, new DateOnly(2026, 7, 1)));
            await dbContext.SaveChangesAsync();
        }

        var delete = await client.DeleteAsync($"/api/v1/categories/{id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }
}
