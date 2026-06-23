namespace Osiris.Application.Common.AI;

/// <summary>
/// A citation the assistant can surface to the user (the account, statement, bill, etc. a number
/// came from). Lets the UI deep-link and lets the user verify the agent did not invent values.
/// </summary>
public sealed record AiSource(string Type, string? Id, string Label);

/// <summary>
/// The compact, server-shaped output of a tool. <see cref="ResultJson"/> is what is handed back to the
/// model — it must be small, typed and free of raw EF entities. Failures carry a safe message only.
/// </summary>
public sealed record AiToolResult(
    bool IsSuccess,
    string ResultJson,
    string? ErrorMessage = null,
    IReadOnlyList<AiSource>? Sources = null)
{
    public static AiToolResult Success(string resultJson, IReadOnlyList<AiSource>? sources = null) =>
        new(true, resultJson, null, sources ?? Array.Empty<AiSource>());

    public static AiToolResult Failure(string errorMessage) =>
        new(false, "{\"error\":true}", errorMessage, Array.Empty<AiSource>());
}
