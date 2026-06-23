using Osiris.Application.Common.Models;
using Osiris.Application.Features.CreditCardPurchases.Commands.ChangeCreditCardPurchaseCategory;
using Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases;

public sealed class ChangeCreditCardPurchaseCategoryCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly FakeCreditCardInstallmentRepository _installments = new();
    private readonly FakeCreditCardStatementRepository _statements;
    private readonly FakeCreditCardPurchaseRepository _purchases;
    private readonly FakeCategoryRepository _categories = new();
    private readonly ChangeCreditCardPurchaseCategoryCommandHandler _handler;

    public ChangeCreditCardPurchaseCategoryCommandHandlerTests()
    {
        _statements = new FakeCreditCardStatementRepository(_installments);
        _purchases = new FakeCreditCardPurchaseRepository(_installments, _statements);
        _handler = new ChangeCreditCardPurchaseCategoryCommandHandler(
            _purchases,
            _categories,
            new FakeCurrentUser(_tenantId),
            new FakeDateTimeProvider { UtcNow = new DateTime(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc) });
    }

    [Fact]
    public async Task Handle_ReassignsTheCategory_WhenTargetIsAnActiveExpense()
    {
        var (purchase, _) = SeedPurchase();
        var target = SeedCategory("Lazer", CategoryType.Expense);

        var result = await _handler.Handle(
            new ChangeCreditCardPurchaseCategoryCommand(purchase.Id, target.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, purchase.CategoryId);
        Assert.NotNull(purchase.UpdatedAtUtc);
    }

    [Fact]
    public async Task Handle_WhenPurchaseMissing_ReturnsNotFound()
    {
        var target = SeedCategory("Lazer", CategoryType.Expense);

        var result = await _handler.Handle(
            new ChangeCreditCardPurchaseCategoryCommand(Guid.NewGuid(), target.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCategoryArchived_Fails()
    {
        var (purchase, _) = SeedPurchase();
        var target = SeedCategory("Antiga", CategoryType.Expense);
        target.Archive(DateTime.UtcNow);

        var result = await _handler.Handle(
            new ChangeCreditCardPurchaseCategoryCommand(purchase.Id, target.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhenCategoryIsIncome_Fails()
    {
        var (purchase, _) = SeedPurchase();
        var income = SeedCategory("Salário", CategoryType.Income);

        var result = await _handler.Handle(
            new ChangeCreditCardPurchaseCategoryCommand(purchase.Id, income.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private FinancialCategory SeedCategory(string name, CategoryType type)
    {
        var category = new FinancialCategory(_tenantId, name, type);
        _categories.Add(category);
        return category;
    }

    private (CreditCardPurchase Purchase, FinancialCategory Original) SeedPurchase()
    {
        var original = SeedCategory("Mercado", CategoryType.Expense);
        var purchase = new CreditCardPurchase(
            _tenantId,
            Guid.NewGuid(),
            original.Id,
            "Compra de teste",
            100m,
            new DateOnly(2026, 6, 20),
            1);
        _purchases.Add(purchase);
        return (purchase, original);
    }
}
