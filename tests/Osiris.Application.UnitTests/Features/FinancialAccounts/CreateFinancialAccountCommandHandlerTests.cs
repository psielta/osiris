using Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts;

public sealed class CreateFinancialAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_ShouldCreateAccountWithMatchingBalancesForCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeFinancialAccountRepository();
        var handler = new CreateFinancialAccountCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateFinancialAccountCommand("Banco", FinancialAccountType.CheckingAccount, 200m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var account = Assert.Single(repository.Accounts);
        Assert.Equal(result.Value, account.Id);
        Assert.Equal(tenantId, account.TenantId);
        Assert.Equal("Banco", account.Name);
        Assert.Equal("BANCO", account.NormalizedName);
        Assert.Equal(200m, account.InitialBalance);
        Assert.Equal(200m, account.CurrentBalance);
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameInSameTenant_ShouldFail()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeFinancialAccountRepository();
        repository.Add(new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m));
        var handler = new CreateFinancialAccountCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateFinancialAccountCommand("banco", FinancialAccountType.Cash, 0m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(repository.Accounts);
    }

    [Fact]
    public async Task Handle_WhenSameNameInDifferentTenant_ShouldCreate()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeFinancialAccountRepository();
        repository.Add(new FinancialAccount(Guid.NewGuid(), "Banco", FinancialAccountType.CheckingAccount, 0m));
        var handler = new CreateFinancialAccountCommandHandler(repository, new FakeCurrentUser(tenantId));

        var result = await handler.Handle(
            new CreateFinancialAccountCommand("Banco", FinancialAccountType.Cash, 0m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.Accounts.Count);
    }
}
