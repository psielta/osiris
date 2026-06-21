using Osiris.Application.Features.FinancialAccountMovements.Commands.ImportOfxStatement;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class ImportOfxStatementCommandHandlerTests
{
    private static readonly DateOnly Date = new(2026, 6, 2);
    private static readonly DateTime SeedTime = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ImportsLinesUpdatesBalanceAndSetsExternalId()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, null),
            new("A2", Date, 90m, FinancialAccountMovementType.Expense, "Mercado", null, null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Imported);
        Assert.Equal(0, result.Value.Reconciled);
        Assert.Equal(0, result.Value.SkippedDuplicates);
        Assert.Equal(1510m, account.CurrentBalance);
        Assert.Equal(2, context.Movements.Movements.Count);
        Assert.Contains(context.Movements.Movements, movement => movement.ExternalId == "A1");
    }

    [Fact]
    public async Task Handle_SkipsDuplicatesAcrossReimport()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, null),
        };

        await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);
        var second = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.Equal(0, second.Value!.Imported);
        Assert.Equal(1, second.Value.SkippedDuplicates);
        Assert.Single(context.Movements.Movements);
        Assert.Equal(1500m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_SkipsDuplicatesWithinSameBatch()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("DUP", Date, 10m, FinancialAccountMovementType.Income, "A", null, null),
            new("DUP", Date, 10m, FinancialAccountMovementType.Income, "A", null, null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.Equal(1, result.Value!.Imported);
        Assert.Equal(1, result.Value.SkippedDuplicates);
        Assert.Single(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenCategoryInvalid_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 10m, FinancialAccountMovementType.Income, "A", Guid.NewGuid(), null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_AppliesProvidedCategory()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);
        var category = new FinancialCategory(tenantId, "Salário", CategoryType.Income);
        context.Categories.Add(category);

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", category.Id, null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, context.Movements.Movements.Single().CategoryId);
    }

    [Fact]
    public async Task Handle_ReconcileLinksExistingMovementWithoutChangingBalance()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var manual = await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, manual.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Imported);
        Assert.Equal(1, result.Value.Reconciled);
        Assert.Equal(0, result.Value.SkippedDuplicates);
        Assert.Equal(1600m, account.CurrentBalance); // reconcile must not move the balance
        Assert.Single(context.Movements.Movements); // no new movement created
        Assert.Equal("A1", manual.ExternalId);
        Assert.NotNull(manual.ReconciledAtUtc);
    }

    [Fact]
    public async Task Handle_ReconcileAndNewLineTogether()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var manual = await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, manual.Id),
            new("A2", Date, 90m, FinancialAccountMovementType.Expense, "Mercado", null, null),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Imported);
        Assert.Equal(1, result.Value.Reconciled);
        Assert.Equal(0, result.Value.SkippedDuplicates);
        Assert.Equal(1510m, account.CurrentBalance); // only the new expense moves the balance
        Assert.Equal(2, context.Movements.Movements.Count);
        Assert.Equal("A1", manual.ExternalId);
        Assert.Contains(context.Movements.Movements, movement => movement.ExternalId == "A2");
    }

    [Fact]
    public async Task Handle_RejectsSameTargetReconciledTwice()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var manual = await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, manual.Id),
            new("A2", Date, 1500m, FinancialAccountMovementType.Income, "Salario copia", null, manual.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(1600m, account.CurrentBalance); // nothing persisted
        Assert.Single(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_RejectsAlreadyLinkedTarget()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        var imported = await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario", externalId: "OLD");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, imported.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("OLD", imported.ExternalId); // unchanged
        Assert.Single(context.Movements.Movements);
    }

    [Fact]
    public async Task Handle_WhenReconcileTargetInAnotherAccount_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        var otherAccount = new FinancialAccount(tenantId, "Outra", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);
        context.Accounts.Add(otherAccount);
        var foreignMovement = await SeedAsync(context, otherAccount, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, foreignMovement.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Null(foreignMovement.ExternalId); // not touched
        Assert.Equal(100m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenReconcileLineAlreadyImported_SkipsAndLeavesTargetUnlinked()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        context.Accounts.Add(account);
        await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario", externalId: "A1");
        var manual = await SeedAsync(context, account, tenantId, FinancialAccountMovementType.Income, 1500m, Date, "Salario manual");

        var lines = new List<ImportOfxLineInput>
        {
            new("A1", Date, 1500m, FinancialAccountMovementType.Income, "Salario", null, manual.Id),
        };

        var result = await context.Handler.Handle(new ImportOfxStatementCommand(account.Id, lines), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Reconciled);
        Assert.Equal(1, result.Value.SkippedDuplicates);
        Assert.Null(manual.ExternalId); // the already-imported line is skipped, not reconciled
    }

    private static async Task<FinancialAccountMovement> SeedAsync(
        HandlerContext context,
        FinancialAccount account,
        Guid tenantId,
        FinancialAccountMovementType type,
        decimal amount,
        DateOnly date,
        string description,
        string? externalId = null)
    {
        var movement = new FinancialAccountMovement(tenantId, account.Id, type, amount, date, description, externalId: externalId);
        account.ApplyMovement(type, amount, SeedTime);
        await context.Movements.AddAsync(movement, account, CancellationToken.None);
        return movement;
    }

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Categories = new FakeCategoryRepository();
            Handler = new ImportOfxStatementCommandHandler(
                Accounts,
                Movements,
                Categories,
                new FakeCurrentUser(tenantId),
                new FakeDateTimeProvider());
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public FakeCategoryRepository Categories { get; }

        public ImportOfxStatementCommandHandler Handler { get; }
    }
}
