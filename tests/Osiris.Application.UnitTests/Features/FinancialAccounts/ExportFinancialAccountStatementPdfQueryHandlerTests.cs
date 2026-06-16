using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.FinancialAccountMovements.DTOs;
using Osiris.Application.Features.FinancialAccounts.DTOs;
using Osiris.Application.Features.FinancialAccounts.Queries.ExportFinancialAccountStatementPdf;
using Osiris.Application.UnitTests.Common;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.FinancialAccounts;

public sealed class ExportFinancialAccountStatementPdfQueryHandlerTests
{
    private readonly FakeFinancialAccountStatementPdfRenderer _renderer = new();

    private ExportFinancialAccountStatementPdfQueryHandler CreateHandler(FinancialAccountStatementDto? statement)
        => new(new StubSender(statement), _renderer);

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnNull()
    {
        var handler = CreateHandler(statement: null);

        var result = await handler.Handle(
            new ExportFinancialAccountStatementPdfQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(_renderer.Received);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ShouldReturnRenderedPdf()
    {
        var statement = new FinancialAccountStatementDto(
            Guid.NewGuid(),
            "Conta Corrente Itaú",
            FinancialAccountType.CheckingAccount,
            100m,
            150m,
            IsActive: true,
            Array.Empty<MovementListItemDto>());
        var handler = CreateHandler(statement);

        var result = await handler.Handle(
            new ExportFinancialAccountStatementPdfQuery(statement.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(statement, _renderer.Received);
        Assert.Equal(_renderer.Content, result!.Content);
        Assert.Equal("application/pdf", result.ContentType);
        // Accents are dropped and spaces collapse to dashes.
        Assert.Equal("extrato-conta-corrente-itau.pdf", result.FileName);
    }
}

internal sealed class FakeFinancialAccountStatementPdfRenderer : IFinancialAccountStatementPdfRenderer
{
    public byte[] Content { get; } = { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
    public FinancialAccountStatementDto? Received { get; private set; }

    public byte[] Render(FinancialAccountStatementDto statement)
    {
        Received = statement;
        return Content;
    }
}
