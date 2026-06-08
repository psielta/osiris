using Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts;

public sealed class UpdateFinancialAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateNameAndTypeWithoutChangingBalance()
    {
        var tenantId = Guid.NewGuid();
        var now = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 100m);
        account.ApplyMovement(FinancialAccountMovementType.Income, 50m, now);
        var repository = new FakeFinancialAccountRepository();
        repository.Add(account);
        var handler = new UpdateFinancialAccountCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider { UtcNow = now });

        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, "Reserva", FinancialAccountType.SavingsAccount),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Reserva", account.Name);
        Assert.Equal(FinancialAccountType.SavingsAccount, account.Type);
        Assert.Equal(100m, account.InitialBalance);
        Assert.Equal(150m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToOtherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m);
        var repository = new FakeFinancialAccountRepository();
        repository.Add(account);
        var handler = new UpdateFinancialAccountCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, "Reserva", FinancialAccountType.SavingsAccount),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Banco", account.Name);
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        var other = new FinancialAccount(tenantId, "Reserva", FinancialAccountType.SavingsAccount, 0m);
        var repository = new FakeFinancialAccountRepository();
        repository.Add(account);
        repository.Add(other);
        var handler = new UpdateFinancialAccountCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(
            new UpdateFinancialAccountCommand(account.Id, "Reserva", FinancialAccountType.CheckingAccount),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Banco", account.Name);
    }
}
