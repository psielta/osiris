using System.ComponentModel.DataAnnotations;
using Osiris.Domain.Enums;

namespace Osiris.Web.Models;

/// <summary>
/// What to do with an imported statement line on confirm.
/// </summary>
public enum ImportLineAction
{
    New,
    Reconcile,
    Ignore
}

public sealed class OfxImportLineViewModel
{
    public string ExternalId { get; set; } = string.Empty;

    public DateOnly OccurredOn { get; set; }

    public decimal Amount { get; set; }

    public FinancialAccountMovementType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsDuplicate { get; set; }

    public bool IsInflow => Type == FinancialAccountMovementType.Income;

    [Display(Name = "Ação")]
    public ImportLineAction Action { get; set; } = ImportLineAction.New;

    /// <summary>Existing movement chosen to reconcile with (only used when <see cref="Action"/> is Reconcile).</summary>
    public Guid? ReconcileWithMovementId { get; set; }

    /// <summary>Movement auto-suggested by the matcher, if any.</summary>
    public Guid? SuggestedMatchId { get; set; }

    public IReadOnlyList<ReconciliationCandidateViewModel> Candidates { get; set; } =
        Array.Empty<ReconciliationCandidateViewModel>();

    [Display(Name = "Categoria")]
    public Guid? CategoryId { get; set; }
}
