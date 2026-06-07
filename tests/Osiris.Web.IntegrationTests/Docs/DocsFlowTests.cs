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

        Assert.Contains("Documentacao do Osiris", html);
        Assert.Contains("Guia simples de categorias", html);
        Assert.Contains("href=\"/docs/categories\"", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Show_ShouldRenderMarkdownViewerForSlug()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/docs/categories");

        Assert.Contains("marked.min.js", html);
        Assert.Contains("DOMPurify", html);
        Assert.Contains("data-markdown-url=\"/docs/categories.md\"", html);
        Assert.Contains("Todos os guias", html);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Markdown_ShouldReturnProtectedMarkdownForSlug()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var response = await client.GetAsync("/docs/categories.md");
        var markdown = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/markdown", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("# Guia simples de categorias", markdown);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Show_ShouldMarkDocumentationNavigationAsActive()
    {
        var client = await IntegrationTestHelpers.RegisterAndAuthenticateAsync(_factory);

        var html = await client.GetStringAsync("/docs/categories");

        Assert.Matches(
            new Regex("<a(?=[^>]*href=\"/docs\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
        Assert.DoesNotMatch(
            new Regex("<a(?=[^>]*href=\"/categories\")(?=[^>]*aria-current=\"page\")[^>]*>", RegexOptions.IgnoreCase),
            html);
    }
}
