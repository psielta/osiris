using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;
using Osiris.Application.UnitTests.Features.CreditCardStatementPayments.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCardStatementPayments;

public sealed class RegisterCreditCardStatementPaymentCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardStatementPaymentRepository _payments = new();
    private readonly FakeCreditCardStatementRepository _statements;
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeFinancialAccountRepository _accounts = new();

    private readonly CreditCard _card;
    private readonly CreditCardStatement _statement;

    public RegisterCreditCardStatementPaymentCommandHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_payments);

        _card = new CreditCard(_tenantId, "Nubank", 5000m, 25, 5, null);
        _cards.Add(_card);

        // Statement 06/2026 with R$ 100.00 of installments, closed on 2026-06-25, due 2026-07-05.
        _statement = new CreditCardStatement(
            _tenantId,
            _card.Id,
            6,
            2026,
            new DateOnly(2026, 6, 25),
            new DateOnly(2026, 7, 5));
        _statements.Add(_statement, installmentsTotal: 100m);
    }

    [Fact]
    public async Task Handle_FullPayment_ShouldMarkStatementAsPaid()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 100m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_payments.Payments);
        Assert.Equal(CreditCardStatementStatus.Paid, _statement.Status);
    }

    [Fact]
    public async Task Handle_PartialPayment_ShouldMarkStatementAsPartiallyPaid()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 40m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CreditCardStatementStatus.PartiallyPaid, _statement.Status);
    }

    [Fact]
    public async Task Handle_PartialPaymentPastDueDate_ShouldKeepStatementOverdue()
    {
        var handler = CreateHandler(utcNow: new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));

        var result = await handler.Handle(Command(amount: 40m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CreditCardStatementStatus.Overdue, _statement.Status);
    }

    [Fact]
    public async Task Handle_MultiplePayments_ShouldAccumulateUntilPaid()
    {
        var handler = CreateHandler();

        Assert.True((await handler.Handle(Command(amount: 40m), CancellationToken.None)).IsSuccess);
        Assert.True((await handler.Handle(Command(amount: 60m), CancellationToken.None)).IsSuccess);

        Assert.Equal(2, _payments.Payments.Count);
        Assert.Equal(CreditCardStatementStatus.Paid, _statement.Status);
    }

    [Fact]
    public async Task Handle_WithAccount_ShouldReduceAccountBalance()
    {
        var account = SeedAccount(initialBalance: 500m);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 100m, accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WithAccount_ShouldCreateStatementPaymentMovementWithoutCategory()
    {
        var account = SeedAccount(initialBalance: 500m);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 100m, accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(_payments.Movements);
        Assert.Equal(FinancialAccountMovementType.CreditCardStatementPayment, movement.Type);
        Assert.Equal(100m, movement.Amount);
        Assert.Equal(account.Id, movement.FinancialAccountId);

        // Paying a statement must never duplicate the categorized expense.
        Assert.Null(movement.CategoryId);
        Assert.Equal(nameof(CreditCardStatementPayment), movement.RelatedEntityType);
    }

    [Fact]
    public async Task Handle_WithoutAccount_ShouldNotCreateMovement()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 100m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_payments.Movements);
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsOpenBalance_ShouldBlock()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 150m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_payments.Payments);
        Assert.Equal(CreditCardStatementStatus.Open, _statement.Status);
    }

    [Fact]
    public async Task Handle_WhenStatementHasNoOpenBalance_ShouldBlock()
    {
        var handler = CreateHandler();
        Assert.True((await handler.Handle(Command(amount: 100m), CancellationToken.None)).IsSuccess);

        var result = await handler.Handle(Command(amount: 10m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(_payments.Payments);
    }

    [Fact]
    public async Task Handle_WhenStatementFromAnotherTenant_ShouldReturnNotFound()
    {
        var foreignStatement = new CreditCardStatement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            6,
            2026,
            new DateOnly(2026, 6, 25),
            new DateOnly(2026, 7, 5));
        _statements.Add(foreignStatement, installmentsTotal: 100m);
        var handler = CreateHandler();

        var result = await handler.Handle(
            Command(amount: 50m) with { StatementId = foreignStatement.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_payments.Payments);
    }

    [Fact]
    public async Task Handle_WhenAccountFromAnotherTenant_ShouldRejectWithoutPaying()
    {
        var foreignAccount = new FinancialAccount(
            Guid.NewGuid(),
            "Banco Alheio",
            FinancialAccountType.CheckingAccount,
            500m);
        _accounts.Add(foreignAccount);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 50m, accountId: foreignAccount.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_payments.Payments);
        Assert.Equal(500m, foreignAccount.CurrentBalance);
        Assert.Equal(CreditCardStatementStatus.Open, _statement.Status);
    }

    [Fact]
    public async Task Handle_WhenAccountArchived_ShouldReject()
    {
        var account = SeedAccount(initialBalance: 500m);
        account.Archive(DateTime.UtcNow);
        var handler = CreateHandler();

        var result = await handler.Handle(Command(amount: 50m, accountId: account.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_payments.Payments);
        Assert.Equal(500m, account.CurrentBalance);
    }

    private RegisterCreditCardStatementPaymentCommandHandler CreateHandler(DateTime? utcNow = null)
    {
        return new RegisterCreditCardStatementPaymentCommandHandler(
            _statements,
            _payments,
            _cards,
            _accounts,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider
            {
                UtcNow = utcNow ?? new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc)
            });
    }

    private FinancialAccount SeedAccount(decimal initialBalance)
    {
        var account = new FinancialAccount(
            _tenantId,
            "Banco Principal",
            FinancialAccountType.CheckingAccount,
            initialBalance);
        _accounts.Add(account);
        return account;
    }

    private RegisterCreditCardStatementPaymentCommand Command(decimal amount, Guid? accountId = null)
    {
        return new RegisterCreditCardStatementPaymentCommand(
            _statement.Id,
            amount,
            new DateOnly(2026, 6, 28),
            accountId,
            Notes: null);
    }
}
