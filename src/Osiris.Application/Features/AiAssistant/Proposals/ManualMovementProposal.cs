using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.AiAssistant.Proposals;

/// <summary>Stable identifiers for the supported proposal action types (stored in <c>AiActionProposal.ActionType</c>).</summary>
public static class AiActionTypes
{
    public const string ManualMovement = "manual_movement";
    public const string BillCreation = "bill_creation";
    public const string CardPurchase = "card_purchase";
    public const string BillPayment = "bill_payment";
    public const string StatementPayment = "statement_payment";
    public const string CategoryChange = "category_change";
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

public sealed record BillCreationPayload(
    string Description,
    decimal Amount,
    DateOnly DueDate,
    Guid CategoryId,
    Guid? PaymentAccountId,
    string? Notes);

public sealed record CardPurchasePayload(
    Guid CreditCardId,
    Guid? CategoryId,
    string Description,
    decimal TotalAmount,
    DateOnly PurchaseDate,
    int Installments,
    string? Notes);

public sealed record BillPaymentPayload(
    Guid BillId,
    DateOnly PaidAt,
    Guid? PaymentAccountId);

public sealed record StatementPaymentPayload(
    Guid StatementId,
    decimal Amount,
    DateOnly PaidAt,
    Guid? FinancialAccountId,
    string? Notes);

public sealed record CategoryChangePayload(
    Guid PurchaseId,
    Guid CategoryId);

/// <summary>
/// Computes the base-state hash used to detect a stale proposal: if the snapshot the proposal was based
/// on changes between proposing and confirming, confirmation is refused. Creation actions hash the
/// payload (nothing to go stale); mutation actions hash the target entity's mutable state.
/// </summary>
public static class ProposalState
{
    public static string AccountHash(decimal currentBalance, bool isActive) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"acct|{currentBalance}|{isActive}"));

    public static string PayloadHash(string payloadJson) =>
        Hash($"payload|{payloadJson}");

    public static string BillHash(DateOnly? paidAt, decimal amount) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"bill|{paidAt is not null}|{amount}"));

    public static string StatementHash(decimal openBalance, CreditCardStatementStatus status) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"stmt|{openBalance}|{(int)status}"));

    public static string PurchaseCategoryHash(Guid currentCategoryId) =>
        Hash($"purchase-category|{currentCategoryId}");

    private static string Hash(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
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
