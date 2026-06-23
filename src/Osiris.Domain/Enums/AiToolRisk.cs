namespace Osiris.Domain.Enums;

/// <summary>
/// Risk classification that drives whether the agent may execute a tool automatically.
/// Read-only tools run during a turn; everything that mutates data must become a confirmable
/// proposal (or is simply not exposed) — the model never writes directly.
/// </summary>
public enum AiToolRisk
{
    /// <summary>Pure read; safe to run automatically inside a turn.</summary>
    ReadOnly = 1,

    /// <summary>Produces a confirmable proposal; never mutates data during the turn.</summary>
    WriteProposal = 2,

    /// <summary>Sensitive operation gated behind extra policy; not exposed in the MVP.</summary>
    Restricted = 3,

    /// <summary>Never executed regardless of model output.</summary>
    Forbidden = 4
}
