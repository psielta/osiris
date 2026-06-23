namespace Osiris.Domain.Enums;

/// <summary>
/// Lifecycle of a write proposal. A proposal is created Pending and only ever mutates data after the
/// user confirms it; the model itself never advances this state.
/// </summary>
public enum AiActionProposalStatus
{
    Pending = 1,
    Confirmed = 2,
    Rejected = 3,
    Expired = 4,
    Stale = 5,
    Executing = 6,
    Executed = 7,
    Failed = 8
}
