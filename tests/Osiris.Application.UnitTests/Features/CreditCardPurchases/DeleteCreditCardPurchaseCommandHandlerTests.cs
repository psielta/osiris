using Osiris.Application.Features.CreditCardPurchases.Commands.DeleteCreditCardPurchase;
using Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class DeleteCreditCardPurchaseCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardInstallmentRepository _installments = new();
    private readonly FakeCreditCardStatementRepository _statements;
    private readonly FakeCreditCardPurchaseRepository _purchases;
    private readonly DeleteCreditCardPurchaseCommandHandler _handler;

    public DeleteCreditCardPurchaseCommandHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_installments);
        _purchases = new FakeCreditCardPurchaseRepository(_installments, _statements);
        _handler = new DeleteCreditCardPurchaseCommandHandler(
            _purchases,
            _installments,
            _statements,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    [Fact]
    public async Task Handle_ShouldDeletePurchaseAndItsInstallments()
    {
        var (purchase, _) = SeedPurchase(installmentCount: 3);

        var result = await _handler.Handle(new DeleteCreditCardPurchaseCommand(purchase.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_purchases.Purchases);
        Assert.Empty(_installments.Installments);
    }

    [Fact]
    public async Task Handle_WhenRelatedStatementIsPaid_ShouldFail()
    {
        var (purchase, statement) = SeedPurchase(installmentCount: 1);
        statement.RefreshStatus(
            totalAmount: 100m,
            paidAmount: 100m,
            today: new DateOnly(2026, 6, 26),
            utcNow: DateTime.UtcNow);
        Assert.Equal(CreditCardStatementStatus.Paid, statement.Status);

        var result = await _handler.Handle(new DeleteCreditCardPurchaseCommand(purchase.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(_purchases.Purchases);
        Assert.Single(_installments.Installments);
    }

    [Fact]
    public async Task Handle_WhenPurchaseFromAnotherTenant_ShouldReturnNotFound()
    {
        var (purchase, _) = SeedPurchase(installmentCount: 1, tenantId: Guid.NewGuid());

        var result = await _handler.Handle(new DeleteCreditCardPurchaseCommand(purchase.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_ShouldRefreshStatementStatusAfterRemovingInstallments()
    {
        // Statement past due with an open balance reads Overdue; once the only purchase is
        // removed the balance is gone and it must fall back to Closed.
        var (purchase, statement) = SeedPurchase(installmentCount: 1);
        statement.RefreshStatus(100m, 0m, new DateOnly(2026, 7, 10), DateTime.UtcNow);
        Assert.Equal(CreditCardStatementStatus.Overdue, statement.Status);

        var handler = new DeleteCreditCardPurchaseCommandHandler(
            _purchases,
            _installments,
            _statements,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc) });

        var result = await handler.Handle(new DeleteCreditCardPurchaseCommand(purchase.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CreditCardStatementStatus.Closed, statement.Status);
    }

    private (CreditCardPurchase Purchase, CreditCardStatement Statement) SeedPurchase(
        int installmentCount,
        Guid? tenantId = null)
    {
        var owner = tenantId ?? _tenantId;
        var cardId = Guid.NewGuid();
        var statement = new CreditCardStatement(
            owner,
            cardId,
            6,
            2026,
            new DateOnly(2026, 6, 25),
            new DateOnly(2026, 7, 5));
        _statements.Add(statement);

        var purchase = new CreditCardPurchase(
            owner,
            cardId,
            Guid.NewGuid(),
            "Compra de teste",
            100m,
            new DateOnly(2026, 6, 20),
            installmentCount);
        _purchases.Add(purchase);

        var amounts = Enumerable.Repeat(100m / installmentCount, installmentCount).ToArray();
        _installments.AddRange(amounts.Select((amount, index) => new CreditCardInstallment(
            owner,
            purchase.Id,
            statement.Id,
            index + 1,
            installmentCount,
            amount,
            statement.DueDate)));

        return (purchase, statement);
    }
}
