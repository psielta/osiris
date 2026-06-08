using Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts;

public sealed class ArchiveFinancialAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenExists_ShouldArchive()
    {
        var tenantId = Guid.NewGuid();
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        var repository = new FakeFinancialAccountRepository();
        repository.Add(account);
        var handler = new ArchiveFinancialAccountCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(new ArchiveFinancialAccountCommand(account.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(account.IsActive);
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToOtherTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var account = new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m);
        var repository = new FakeFinancialAccountRepository();
        repository.Add(account);
        var handler = new ArchiveFinancialAccountCommandHandler(
            repository,
            new FakeCurrentUser(tenantId),
            new FakeDateTimeProvider());

        var result = await handler.Handle(new ArchiveFinancialAccountCommand(account.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(account.IsActive);
    }
}
