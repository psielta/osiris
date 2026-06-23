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
    public const string CategoryCreation = "category_creation";
    public const string CategoryUpdate = "category_update";
    public const string CategoryArchive = "category_archive";
    public const string CategoryDeletion = "category_deletion";
    public const string AccountCreation = "account_creation";
    public const string AccountUpdate = "account_update";
    public const string AccountArchive = "account_archive";
    public const string CardCreation = "card_creation";
    public const string CardUpdate = "card_update";
    public const string CardArchive = "card_archive";
    public const string BillUpdate = "bill_update";
    public const string BillDeletion = "bill_deletion";
    public const string BillUnpay = "bill_unpay";
    public const string PurchaseDeletion = "purchase_deletion";
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

public sealed record CategoryCreationPayload(
    string Name,
    string Type,
    string? Color);

public sealed record CategoryUpdatePayload(
    Guid CategoryId,
    string Name,
    string Type,
    string? Color);

public sealed record CategoryRefPayload(Guid CategoryId);

public sealed record AccountCreationPayload(
    string Name,
    string Type,
    decimal InitialBalance);

public sealed record AccountUpdatePayload(
    Guid AccountId,
    string Name,
    string Type);

public sealed record AccountRefPayload(Guid AccountId);

public sealed record CardCreationPayload(
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    Guid? PaymentAccountId);

public sealed record CardUpdatePayload(
    Guid CardId,
    string Name,
    decimal Limit,
    int ClosingDay,
    int DueDay,
    Guid? PaymentAccountId);

public sealed record CardRefPayload(Guid CardId);

public sealed record BillUpdatePayload(
    Guid BillId,
    string Description,
    decimal Amount,
    DateOnly DueDate,
    Guid CategoryId,
    Guid? PaymentAccountId,
    string? Notes);

public sealed record BillRefPayload(Guid BillId);

public sealed record PurchaseRefPayload(Guid PurchaseId);

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

    public static string CategoryHash(string name, CategoryType type, string? color, bool isActive) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"category|{name}|{(int)type}|{color}|{isActive}"));

    // Account profile hash deliberately excludes the balance: the balance moves with every transaction,
    // and an archive/rename proposal should not go stale just because money came in or out meanwhile.
    public static string AccountProfileHash(string name, FinancialAccountType type, bool isActive) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"account|{name}|{(int)type}|{isActive}"));

    public static string CardHash(string name, decimal limit, int closingDay, int dueDay) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"card|{name}|{limit}|{closingDay}|{dueDay}"));

    public static string BillEditHash(string description, decimal amount, DateOnly dueDate, Guid categoryId, bool isPaid) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"bill-edit|{description}|{amount}|{dueDate:O}|{categoryId}|{isPaid}"));

    public static string PurchaseHash(decimal totalAmount) =>
        Hash(string.Create(CultureInfo.InvariantCulture, $"purchase|{totalAmount}"));

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
