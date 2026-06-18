using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Osiris.Web.IntegrationTests.Support;

namespace Osiris.Web.IntegrationTests.Docs;

[Collection(WebIntegrationTestCollection.Name)]
public sealed class DocsFlowTests : IAsyncLifetime
{
    private readonly OsirisWebApplicationFactory _factory;

    public DocsFlowTests(OsirisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        return _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAnonymous_ShouldRedirectToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/docs");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            "http://localhost/Account/Login?ReturnUrl=%2Fdocs",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Index_WhenAuthenticated_ShouldListDocumentationCatalog()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/docs");

        Assert.Contains("Documentação do Osiris", html);
        Assert.Contains("Primeiros passos no Osiris", html);
        Assert.Contains("Como ler o painel", html);
        Assert.Contains("href=\"/docs/categories\"", html);
        Assert.Contains("href=\"/docs/accounts\"", html);
        Assert.Contains("href=\"/docs/cards\"", html);
        Assert.Contains("href=\"/docs/purchases\"", html);
        Assert.Contains("href=\"/docs/statements\"", html);
        Assert.Contains("href=\"/docs/bills\"", html);
        Assert.Contains("href=\"/docs/reports\"", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Show_ShouldRenderMarkdownViewerForSlug()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/docs/accounts");

        Assert.Contains("marked.min.js", html);
        Assert.Contains("DOMPurify", html);
        Assert.Contains("data-markdown-url=\"/docs/accounts.md\"", html);
        Assert.Contains("Todos os guias", html);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("getting-started", "# Primeiros passos no Osiris")]
    [InlineData("dashboard", "# Como ler o painel")]
    [InlineData("categories", "# Guia simples de categorias")]
    [InlineData("accounts", "# Guia simples de contas")]
    [InlineData("cards", "# Guia simples de cartões")]
    [InlineData("purchases", "# Compras no cartão")]
    [InlineData("statements", "# Faturas de cartão")]
    [InlineData("bills", "# Contas a pagar")]
    [InlineData("reports", "# Relatórios")]
    public async Task Markdown_ShouldReturnProtectedMarkdownForSlug(string slug, string heading)
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync($"/docs/{slug}.md");
        var markdown = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/markdown", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(heading, markdown);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Show_ShouldExposeDocumentationHelpButtonInHeader()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/docs/categories");

        Assert.Contains("aria-label=\"Abrir ajuda\"", html);
        Assert.Contains("ph ph-question", html);
        Assert.Contains("href=\"/docs\"", html);
        Assert.DoesNotContain(">Documentação</a>", html);
        Assert.DoesNotMatch(
            new Regex("<a(?=[^>]*href=\"/docs\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
        Assert.DoesNotMatch(
            new Regex("<a(?=[^>]*href=\"/categories\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
    }
}
