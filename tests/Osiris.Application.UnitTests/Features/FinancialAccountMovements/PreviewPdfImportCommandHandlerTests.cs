using System.Text;
using Osiris.Application.Common.Pdf;
using Osiris.Application.Features.FinancialAccountMovements.Commands.PreviewPdfImport;
using Osiris.Application.UnitTests.Features.FinancialAccounts.Support;
using Osiris.Domain.Entities;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccountMovements;

public sealed class PreviewPdfImportCommandHandlerTests
{
    private static readonly byte[] Pdf = Encoding.UTF8.GetBytes("%PDF-1.4 fake");

    private static IReadOnlyList<ExtractedStatementTransaction> TwoTransactions() => new[]
    {
        new ExtractedStatementTransaction("pdf-1", new DateOnly(2026, 2, 1), 1500m, FinancialAccountMovementType.Income, "Salario"),
        new ExtractedStatementTransaction("pdf-2", new DateOnly(2026, 2, 2), 90m, FinancialAccountMovementType.Expense, "Mercado"),
    };

    [Fact]
    public async Task Handle_ReturnsPreviewLines()
    {
        var context = Context(out var account, new FakePdfStatementExtractor(TwoTransactions()));

        var result = await context.Handler.Handle(
            new PreviewPdfImportCommand(account.Id, Pdf, "extrato.pdf"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.NewCount);
        Assert.Equal(0, result.Value.DuplicateCount);
    }

    [Fact]
    public async Task Handle_FlagsAlreadyImported()
    {
        var context = Context(out var account, new FakePdfStatementExtractor(TwoTransactions()));

        await context.Movements.AddAsync(
            new FinancialAccountMovement(
                account.TenantId,
                account.Id,
                FinancialAccountMovementType.Income,
                1500m,
                new DateOnly(2026, 2, 1),
                "Salario",
                externalId: "pdf-1"),
            account,
            CancellationToken.None);

        var result = await context.Handler.Handle(
            new PreviewPdfImportCommand(account.Id, Pdf, "extrato.pdf"),
            CancellationToken.None);

        Assert.Equal(1, result.Value!.DuplicateCount);
        Assert.True(result.Value.Lines.Single(line => line.ExternalId == "pdf-1").IsDuplicate);
    }

    [Fact]
    public async Task Handle_WhenNoTransactions_Fails()
    {
        var context = Context(out var account, new FakePdfStatementExtractor([]));

        var result = await context.Handler.Handle(
            new PreviewPdfImportCommand(account.Id, Pdf, "extrato.pdf"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhenExtractorThrows_Fails()
    {
        var context = Context(out var account, FakePdfStatementExtractor.Throwing());

        var result = await context.Handler.Handle(
            new PreviewPdfImportCommand(account.Id, Pdf, "extrato.pdf"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static HandlerContext Context(out FinancialAccount account, IPdfStatementExtractor extractor)
    {
        var tenantId = Guid.NewGuid();
        var context = new HandlerContext(tenantId, extractor);
        account = new FinancialAccount(tenantId, "Banco", FinancialAccountType.CheckingAccount, 0m);
        context.Accounts.Add(account);
        return context;
    }

    private sealed class HandlerContext
    {
        public HandlerContext(Guid tenantId, IPdfStatementExtractor extractor)
        {
            Accounts = new FakeFinancialAccountRepository();
            Movements = new FakeFinancialAccountMovementRepository();
            Handler = new PreviewPdfImportCommandHandler(
                Accounts,
                Movements,
                extractor,
                new FakeCurrentUser(tenantId));
        }

        public FakeFinancialAccountRepository Accounts { get; }

        public FakeFinancialAccountMovementRepository Movements { get; }

        public PreviewPdfImportCommandHandler Handler { get; }
    }
}
