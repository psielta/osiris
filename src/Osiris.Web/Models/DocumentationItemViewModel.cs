namespace Osiris.Web.Models;

public sealed class DocumentationItemViewModel
{
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string Icon { get; set; } = "ph ph-file-text";

    public string MarkdownFile { get; set; } = string.Empty;

    public int Order { get; set; }
}
