using System.Text;
using Osiris.Application.Common.Ofx;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewOfxImport;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class PreviewOfxImportCommandHandlerTests
{
    private const string Sample = """
        <OFX>
        <BANKTRANLIST>
        <STMTTRN><TRNTYPE>CREDIT<DTPOSTED>20260602<TRNAMT>1500.00<FITID>A1<MEMO>Salario</STMTTRN>
        <STMTTRN><TRNTYPE>DEBIT<DTPOSTED>20260605<TRNAMT>-90.00<FITID>A2<MEMO>Mercado</STMTTRN>
        </BANKTRANLIST>
        </OFX>
        """;

    [Fact]
    public async Task Handle_ReturnsPreviewWithLines()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new PreviewOfxImportCommand(account.Id, Bytes(Sample), "extrato.ofx"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var preview = result.Value!;
        Assert.Equal(2, preview.TotalCount);
        Assert.Equal(2, preview.NewCount);
        Assert.Equal(0, preview.DuplicateCount);
        Assert.All(preview.Lines, line => Assert.False(line.IsDuplicate));
    }

    [Fact]
    public async Task Handle_FlagsAlreadyImportedTransactions()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        await context.Movements.AddAsync(
            new FinancialAccountMovement(
                tenantId,
                account.Id,
                FinancialAccountMovementType.Income,
                1500m,
                new DateOnly(2026, 6, 2),
                "Salario",
                externalId: "A1"),
            account,
            CancellationToken.None);

        var result = await context.Handler.Handle(
            new PreviewOfxImportCommand(account.Id, Bytes(Sample), "extrato.ofx"),
            CancellationToken.None);

        var preview = result.Value!;
        Assert.Equal(1, preview.DuplicateCount);
        Assert.True(preview.Lines.Single(line => line.ExternalId == "A1").IsDuplicate);
        Assert.False(preview.Lines.Single(line => line.ExternalId == "A2").IsDuplicate);
    }

    [Fact]
    public async Task Handle_WhenAccountMissing_Fails()
    {
        var context = new HandlerContext(Guid.NewGuid());

        var result = await context.Handler.Handle(
            new PreviewOfxImportCommand(Guid.NewGuid(), Bytes(Sample), "extrato.ofx"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_Fails()
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId);
        var account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);

        var result = await context.Handler.Handle(
            new PreviewOfxImportCommand(account.Id, Bytes("nao e ofx"), "extrato.ofx"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Handler = new PreviewOfxImportCommandHandler(
                Accounts,
                Movements,
                new OfxStatementParser(),
                new FakeCurrentUser(tenantId));
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public PreviewOfxImportCommandHandler Handler { get; }
    }
}
