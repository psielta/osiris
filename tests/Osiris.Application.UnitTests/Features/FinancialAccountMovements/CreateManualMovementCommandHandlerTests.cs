using Osiris.Application.Features.FinancialAccountMovements.Commands.CreateManualMovement;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class CreateManualMovementCommandHandlerTests
{
    private static readonly DateOnly OccurredOn = new(2026, 6, 8);

    [Fact]
    public async Task Handle_WhenIncome_ShouldIncreaseBalanceAndPersistMovement()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Income, 50m, OccurredOn, "Salário", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(150m, account.CurrentBalance);
        var movement = Assert.Single(context.Movements.Movements);
        Assert.Equal(account.Id, movement.FinancialAccountId);
        Assert.Equal(tenantId, movement.TenantId);
        Assert.Equal(50m, movement.Amount);
        Assert.Equal(OccurredOn, movement.OccurredOn);
        Assert.Equal("Salário", movement.Description);
    }

    [Fact]
    public async Task Handle_WhenExpense_ShouldDecreaseBalance()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Expense, 30m, OccurredOn, "Mercado", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(70m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToAnotherTenant_ShouldFailAndNotCreateMovement()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var foreignAccount = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(foreignAccount);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(foreignAccount.Id, FinancialAccountMovementType.Income, 50m, OccurredOn, "Salário", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
        Assert.Equal(100m, foreignAccount.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenAccountArchived_ShouldFailAndNotChangeBalance()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        account.Archive(DateTime.UtcNow);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Income, 50m, OccurredOn, "Salário", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
        Assert.Equal(100m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenCategoryMissingOrFromAnotherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Expense, 30m, OccurredOn, "Mercado", Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
        Assert.Equal(100m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenCategoryArchived_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var category = new FinancialCategory(tenantId, "Mercado", CategoryType.Expense);
        category.Archive(DateTime.UtcNow);
        context.Categories.Add(category);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Expense, 30m, OccurredOn, "Mercado", category.Id, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenActiveCategoryProvided_ShouldPersistMovementWithCategory()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var category = new FinancialCategory(tenantId, "Mercado", CategoryType.Expense);
        context.Categories.Add(category);

        var result = await context.Handler.Handle(
            new CreateManualMovementCommand(account.Id, FinancialAccountMovementType.Expense, 30m, OccurredOn, "Mercado", category.Id, "feira"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var movement = Assert.Single(context.Movements.Movements);
        Assert.Equal(category.Id, movement.CategoryId);
        Assert.Equal("feira", movement.Notes);
        Assert.Equal(70m, account.CurrentBalance);
    }

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Categories = new FakeCategoryRepository();
            Handler = new CreateManualMovementCommandHandler(
                Accounts,
                Movements,
                Categories,
                new FakeCurrentUser(tenantId),
                new FakeDateTimeProvider());
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public FakeCategoryRepository Categories { get; }

        public CreateManualMovementCommandHandler Handler { get; }
    }
}
