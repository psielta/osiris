using System.Text;
using Osiris.Application.Common.Csv;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewCsvImport;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class PreviewCsvImportCommandHandlerTests
{
    private const string Csv = "data;descricao;valor;id\n01/02/2026;Salario;1500,00;TX-1\n02/02/2026;Mercado;-90,00;TX-2";

    private static CsvImportMapping Mapping() => new()
    {
        HeaderLineIndex = 0,
        AmountMode = CsvAmountMode.SignedAmount,
        DateColumn = 0,
        DescriptionColumn = 1,
        AmountColumn = 2,
        ExternalIdColumn = 3,
    };

    [Fact]
    public async Task Handle_ReturnsPreviewLines()
    {
        var context = Context(out var account);

        var result = await context.Handler.Handle(
            new PreviewCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv", Mapping()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.NewCount);
        Assert.Equal(0, result.Value.DuplicateCount);
    }

    [Fact]
    public async Task Handle_FlagsAlreadyImported()
    {
        var context = Context(out var account);

        await context.Movements.AddAsync(
            new FinancialAccountMovement(
                account.TenantId,
                account.Id,
                FinancialAccountMovementType.Income,
                1500m,
                new DateOnly(2026, 2, 1),
                "Salario",
                externalId: "TX-1"),
            account,
            CancellationToken.None);

        var result = await context.Handler.Handle(
            new PreviewCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv", Mapping()),
            CancellationToken.None);

        Assert.Equal(1, result.Value!.DuplicateCount);
        Assert.True(result.Value.Lines.Single(line => line.ExternalId == "TX-1").IsDuplicate);
        Assert.False(result.Value.Lines.Single(line => line.ExternalId == "TX-2").IsDuplicate);
    }

    [Fact]
    public async Task Handle_RemembersMapping()
    {
        var context = Context(out var account);

        await context.Handler.Handle(
            new PreviewCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv", Mapping()),
            CancellationToken.None);

        var preference = Assert.Single(context.Preferences.Preferences);
        Assert.Equal(account.Id, preference.FinancialAccountId);
    }

    [Fact]
    public async Task Handle_WhenMappingProducesNoRows_Fails()
    {
        var context = Context(out var account);

        // Pointing the date column at the description yields no parseable dates, so every row is dropped.
        var badMapping = new CsvImportMapping
        {
            HeaderLineIndex = 0,
            DateColumn = 1,
            DescriptionColumn = 0,
            AmountColumn = 2,
        };

        var result = await context.Handler.Handle(
            new PreviewCsvImportCommand(account.Id, Bytes(Csv), "extrato.csv", badMapping),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static HandlerContext Context(out FinancialAccount account)
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);
        return context;
    }

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Preferences = new FakeCsvImportPreferenceRepository();
            Handler = new PreviewCsvImportCommandHandler(
                Accounts,
                Movements,
                Preferences,
                new CsvStatementParser(),
                new FakeCurrentUser(tenantId),
                new FakeDateTimeProvider());
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public FakeCsvImportPreferenceRepository Preferences { get; }

        public PreviewCsvImportCommandHandler Handler { get; }
    }
}
