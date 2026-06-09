using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardPurchases.Commands.CreateCreditCardPurchase;
using Osiris.Application.Features.CreditCardStatements.Services;
using Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class CreateCreditCardPurchaseCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardRepository _cards = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeCreditCardInstallmentRepository _installments = new();
    private readonly FakeCreditCardStatementRepository _statements;
    private readonly FakeCreditCardPurchaseRepository _purchases;
    private readonly CreateCreditCardPurchaseCommandHandler _handler;

    private readonly CreditCard _card;
    private readonly FinancialCategory _expenseCategory;

    public CreateCreditCardPurchaseCommandHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_installments);
        _purchases = new FakeCreditCardPurchaseRepository(_installments, _statements);

        _card = new CreditCard(_tenantId, "Nubank", 5000m, 25, 5, null);
        _cards.Add(_card);
        _expenseCategory = new FinancialCategory(_tenantId, "Mercado", CategoryType.Expense);
        _categories.Add(_expenseCategory);

        _handler = new CreateCreditCardPurchaseCommandHandler(
            _cards,
            _categories,
            _purchases,
            _statements,
            new CreditCardStatementResolver(_statements),
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider());
    }

    [Fact]
    public async Task Handle_CashPurchase_ShouldCreateSingleInstallmentInCorrectStatement()
    {
        var result = await _handler.Handle(
            Command(totalAmount: 100m, purchaseDate: new DateOnly(2026, 6, 20), installments: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var purchase = Assert.Single(_purchases.Purchases);
        Assert.Equal(_tenantId, purchase.TenantId);
        Assert.Equal(1, purchase.Installments);

        var installment = Assert.Single(_installments.Installments);
        Assert.Equal(100m, installment.Amount);
        Assert.Equal(1, installment.InstallmentNumber);
        Assert.Equal(1, installment.TotalInstallments);

        var statement = Assert.Single(_statements.Statements);
        Assert.Equal(6, statement.ReferenceMonth);
        Assert.Equal(2026, statement.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 6, 25), statement.ClosingDate);
        Assert.Equal(new DateOnly(2026, 7, 5), statement.DueDate);
        Assert.Equal(statement.Id, installment.CreditCardStatementId);
        Assert.Equal(statement.DueDate, installment.DueDate);
    }

    [Fact]
    public async Task Handle_SixInstallments_ShouldCreateSixInstallmentsAcrossConsecutiveStatements()
    {
        var result = await _handler.Handle(
            Command(totalAmount: 100m, purchaseDate: new DateOnly(2026, 6, 20), installments: 6),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, _installments.Installments.Count);
        Assert.Equal(6, _statements.Statements.Count);

        var references = _statements.Statements
            .OrderBy(statement => statement.ReferenceYear)
            .ThenBy(statement => statement.ReferenceMonth)
            .Select(statement => (statement.ReferenceYear, statement.ReferenceMonth))
            .ToArray();
        Assert.Equal(
            new[] { (2026, 6), (2026, 7), (2026, 8), (2026, 9), (2026, 10), (2026, 11) },
            references);
    }

    [Fact]
    public async Task Handle_Installments_ShouldSumExactlyToTotalAmount()
    {
        await _handler.Handle(
            Command(totalAmount: 100m, purchaseDate: new DateOnly(2026, 6, 20), installments: 3),
            CancellationToken.None);

        Assert.Equal(100m, _installments.Installments.Sum(installment => installment.Amount));
    }

    [Fact]
    public async Task Handle_CentsRemainder_ShouldGoToLastInstallment()
    {
        await _handler.Handle(
            Command(totalAmount: 100m, purchaseDate: new DateOnly(2026, 6, 20), installments: 3),
            CancellationToken.None);

        var ordered = _installments.Installments
            .OrderBy(installment => installment.InstallmentNumber)
            .Select(installment => installment.Amount)
            .ToArray();
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, ordered);
    }

    [Fact]
    public async Task Handle_PurchaseBeforeClosing_ShouldEnterCurrentStatement()
    {
        await _handler.Handle(
            Command(totalAmount: 50m, purchaseDate: new DateOnly(2026, 6, 25), installments: 1),
            CancellationToken.None);

        var statement = Assert.Single(_statements.Statements);
        Assert.Equal(6, statement.ReferenceMonth);
    }

    [Fact]
    public async Task Handle_PurchaseAfterClosing_ShouldEnterNextStatement()
    {
        await _handler.Handle(
            Command(totalAmount: 50m, purchaseDate: new DateOnly(2026, 6, 26), installments: 1),
            CancellationToken.None);

        var statement = Assert.Single(_statements.Statements);
        Assert.Equal(7, statement.ReferenceMonth);
        Assert.Equal(2026, statement.ReferenceYear);
        Assert.Equal(new DateOnly(2026, 7, 25), statement.ClosingDate);
        Assert.Equal(new DateOnly(2026, 8, 5), statement.DueDate);
    }

    [Fact]
    public async Task Handle_WhenStatementAlreadyExists_ShouldReuseIt()
    {
        await _handler.Handle(
            Command(totalAmount: 50m, purchaseDate: new DateOnly(2026, 6, 20), installments: 1),
            CancellationToken.None);
        await _handler.Handle(
            Command(totalAmount: 70m, purchaseDate: new DateOnly(2026, 6, 21), installments: 1),
            CancellationToken.None);

        var statement = Assert.Single(_statements.Statements);
        Assert.Equal(2, _installments.Installments.Count);
        Assert.All(
            _installments.Installments,
            installment => Assert.Equal(statement.Id, installment.CreditCardStatementId));
    }

    [Fact]
    public async Task Handle_WhenCategoryIsIncome_ShouldFail()
    {
        var incomeCategory = new FinancialCategory(_tenantId, "Salário", CategoryType.Income);
        _categories.Add(incomeCategory);

        var result = await _handler.Handle(
            Command(categoryId: incomeCategory.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_WhenCategoryFromAnotherTenant_ShouldFail()
    {
        var foreignCategory = new FinancialCategory(Guid.NewGuid(), "Mercado", CategoryType.Expense);
        _categories.Add(foreignCategory);

        var result = await _handler.Handle(
            Command(categoryId: foreignCategory.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_WhenCategoryArchived_ShouldFail()
    {
        _expenseCategory.Archive(DateTime.UtcNow);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_WhenCardFromAnotherTenant_ShouldFail()
    {
        var foreignCard = new CreditCard(Guid.NewGuid(), "Inter", 1000m, 10, 20, null);
        _cards.Add(foreignCard);

        var result = await _handler.Handle(
            Command(creditCardId: foreignCard.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_WhenCardArchived_ShouldFail()
    {
        _card.Archive(DateTime.UtcNow);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
    }

    [Fact]
    public async Task Handle_WhenTotalTooSmallForInstallments_ShouldFail()
    {
        var result = await _handler.Handle(
            Command(totalAmount: 0.01m, installments: 2),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_purchases.Purchases);
        Assert.Empty(_installments.Installments);
        Assert.Empty(_statements.Statements);
    }

    [Fact]
    public void Handler_ShouldNotDependOnFinancialAccountsOrMovements()
    {
        // Card purchases must never touch cash: paying the statement is the only cash outflow.
        var parameters = typeof(CreateCreditCardPurchaseCommandHandler)
            .GetConstructors()
            .Single()
            .GetParameters();

        Assert.DoesNotContain(parameters, parameter =>
            parameter.ParameterType == typeof(IFinancialAccountRepository)
            || parameter.ParameterType == typeof(IFinancialAccountMovementRepository));
    }

    private CreateCreditCardPurchaseCommand Command(
        Guid? creditCardId = null,
        Guid? categoryId = null,
        decimal? totalAmount = 100m,
        DateOnly? purchaseDate = null,
        int? installments = 1)
    {
        return new CreateCreditCardPurchaseCommand(
            creditCardId ?? _card.Id,
            categoryId ?? _expenseCategory.Id,
            "Compra de teste",
            totalAmount,
            purchaseDate ?? new DateOnly(2026, 6, 20),
            installments,
            Notes: null);
    }
}
