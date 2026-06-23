namespace Osiris.Application.Common.AI;

/// <summary>
/// A citation the assistant can surface to the user (the account, statement, bill, etc. a number
/// came from). Lets the UI deep-link and lets the user verify the agent did not invent values.
/// </summary>
public sealed record AiSource(string Type, string? Id, string Label);

/// <summary>
/// A reference to a confirmable write proposal created during a turn. Surfaced (not executed) so the
/// transport layer can render a confirm/reject card and the model can describe it to the user.
/// </summary>
public sealed record AiProposalReference(
    Guid Id,
    string ActionType,
    string DisplaySummary,
    string ImpactSummary,
    string RiskLevel,
    DateTime ExpiresAtUtc);

/// <summary>
/// The compact, server-shaped output of a tool. <see cref="ResultJson"/> is what is handed back to the
/// model — it must be small, typed and free of raw EF entities. Failures carry a safe message only.
/// Write proposals never mutate data here; they surface in <see cref="Proposals"/> for confirmation.
/// </summary>
public sealed record AiToolResult(
    bool IsSuccess,
    string ResultJson,
    string? ErrorMessage = null,
    IReadOnlyList<AiSource>? Sources = null,
    IReadOnlyList<AiProposalReference>? Proposals = null)
{
    public static AiToolResult Success(
        string resultJson,
        IReadOnlyList<AiSource>? sources = null,
        IReadOnlyList<AiProposalReference>? proposals = null) =>
        new(true, resultJson, null, sources ?? Array.Empty<AiSource>(), proposals ?? Array.Empty<AiProposalReference>());

    public static AiToolResult Failure(string errorMessage) =>
        new(false, "{\"error\":true}", errorMessage, Array.Empty<AiSource>(), Array.Empty<AiProposalReference>());
}
