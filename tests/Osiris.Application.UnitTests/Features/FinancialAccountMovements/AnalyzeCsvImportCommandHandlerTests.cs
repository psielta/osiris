using System.Text;
using Osiris.Application.Common.Csv;
using Osiris.Application.Features.FinancialAccountMovements.Commands.AnalyzeCsvImport;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class AnalyzeCsvImportCommandHandlerTests
{
    private const string Csv = "data;descricao;valor\n01/02/2026;Salario;1500,00\n02/02/2026;Mercado;-90,00";

    [Fact]
    public async Task Handle_ReturnsAnalysis()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new AnalyzeCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(";", result.Value!.Delimiter);
        Assert.NotEmpty(result.Value.SampleRows);
        Assert.Null(result.Value.SavedMapping);
    }

    [Fact]
    public async Task Handle_ReturnsSavedMapping_WhenPresent()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var mapping = new CsvImportMapping { DateColumn = 0, DescriptionColumn = 1, AmountColumn = 2 };
        context.Preferences.Add(new CsvImportPreference(tenantId, account.Id, CsvImportMappingSerializer.Serialize(mapping)));

        var result = await context.Handler.Handle(
            new AnalyzeCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv"),
            CancellationToken.None);

        Assert.NotNull(result.Value!.SavedMapping);
        Assert.Equal(2, result.Value.SavedMapping!.AmountColumn);
    }

    [Fact]
    public async Task Handle_WhenAccountMissing_Fails()
    {
        var context = new HandlerContext(Guid.NewGuid());

        var result = await context.Handler.Handle(
            new AnalyzeCsvImportCommand(Guid.NewGuid(), Bytes(Csv), "extrato.csv"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Preferences = new FakeCsvImportPreferenceRepository();
            Handler = new AnalyzeCsvImportCommandHandler(
                Accounts,
                Preferences,
                new CsvStatementParser(),
                new FakeCurrentUser(tenantId));
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeCsvImportPreferenceRepository Preferences { get; }

        public AnalyzeCsvImportCommandHandler Handler { get; }
    }
}
