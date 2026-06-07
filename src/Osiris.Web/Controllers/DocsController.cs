using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Osiris.Web.Models;

namespace Osiris.Web.Controllers;

[Authorize]
[Route("docs")]
public sealed class DocsController : Controller
{
    private const string DocsFolder = "Docs";
    private const string CatalogFileName = "catalog.json";

    private readonly IWebHostEnvironment _environment;

    public DocsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await LoadCatalogAsync(cancellationToken);
        return View(new DocumentationIndexViewModel(items));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Show(string slug, CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        var items = await LoadCatalogAsync(cancellationToken);
        var current = items.SingleOrDefault(item => item.Slug == slug);
        if (current is null)
        {
            return NotFound();
        }

        return View(new DocumentationPageViewModel(current, items));
    }

    [HttpGet("{slug}.md")]
    public async Task<IActionResult> Markdown(string slug, CancellationToken cancellationToken)
    {
        if (!IsValidSlug(slug))
        {
            return NotFound();
        }

        var items = await LoadCatalogAsync(cancellationToken);
        var current = items.SingleOrDefault(item => item.Slug == slug);
        if (current is null)
        {
            return NotFound();
        }

        var docsRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, DocsFolder));
        var docsRootWithSeparator = docsRoot.EndsWith(Path.DirectorySeparatorChar)
            ? docsRoot
            : docsRoot + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(docsRoot, current.MarkdownFile));
        if (!path.StartsWith(docsRootWithSeparator, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
        return Content(markdown, "text/markdown; charset=utf-8");
    }

    private async Task<IReadOnlyCollection<DocumentationItemViewModel>> LoadCatalogAsync(
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_environment.ContentRootPath, DocsFolder, CatalogFileName);
        if (!System.IO.File.Exists(path))
        {
            return Array.Empty<DocumentationItemViewModel>();
        }

        await using var stream = System.IO.File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<DocumentationItemViewModel>>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        return (items ?? new List<DocumentationItemViewModel>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Slug) && !string.IsNullOrWhiteSpace(item.MarkdownFile))
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Title)
            .ToArray();
    }

    private static bool IsValidSlug(string slug)
    {
        return !string.IsNullOrWhiteSpace(slug)
            && slug.All(character => char.IsAsciiLetterLower(character) || char.IsDigit(character) || character == '-');
    }
}
