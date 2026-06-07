namespace Osiris.Web.Models;

public sealed record DocumentationPageViewModel(
    DocumentationItemViewModel Current,
    IReadOnlyCollection<DocumentationItemViewModel> Items);
