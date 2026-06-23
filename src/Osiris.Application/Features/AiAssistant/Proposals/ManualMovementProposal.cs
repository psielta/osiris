using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Osiris.Application.Features.AiAssistant.Proposals;

/// <summary>Stable identifiers for the supported proposal action types (stored in <c>AiActionProposal.ActionType</c>).</summary>
public static class AiActionTypes
{
    public const string ManualMovement = "manual_movement";
}

/// <summary>The validated payload of a manual-movement proposal, serialized into the proposal row.</summary>
public sealed record ManualMovementPayload(
    Guid AccountId,
    string Type,
    decimal Amount,
    DateOnly OccurredOn,
    string Description,
    Guid? CategoryId,
    string? Notes);

/// <summary>
/// Computes the base-state hash used to detect a stale proposal: if the snapshot of the target account
/// changes between proposing and confirming, confirmation is refused.
/// </summary>
public static class ProposalState
{
    public static string AccountHash(decimal currentBalance, bool isActive)
    {
        var seed = string.Create(
            CultureInfo.InvariantCulture,
            $"acct|{currentBalance}|{isActive}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }
}

/// <summary>BRL formatting that does not depend on a pt-BR culture being installed on the host.</summary>
public static class ProposalFormatting
{
    private static readonly NumberFormatInfo Brl = new()
    {
        CurrencySymbol = "R$",
        CurrencyDecimalSeparator = ",",
        CurrencyGroupSeparator = ".",
        CurrencyDecimalDigits = 2,
        CurrencyPositivePattern = 2,
        CurrencyNegativePattern = 9
    };

    public static string Money(decimal value) => value.ToString("C", Brl);
}
