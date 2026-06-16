using Osiris.Application.Common.Interfaces;
using Osiris.Application.Features.CreditCardStatements.DTOs;
using Osiris.Application.Features.CreditCardStatements.Queries.ExportCreditCardStatementPdf;
using Osiris.Application.UnitTests.Common;
using Osiris.Domain.Enums;

namespace Osiris.Application.UnitTests.Features.CreditCardStatements;

public sealed class ExportCreditCardStatementPdfQueryHandlerTests
{
    private readonly FakeCreditCardStatementPdfRenderer _renderer = new();

    private ExportCreditCardStatementPdfQueryHandler CreateHandler(CreditCardStatementDetailsDto? statement)
        => new(new StubSender(statement), _renderer);

    private static CreditCardStatementDetailsDto Statement(Guid creditCardId)
        => new(
            Guid.NewGuid(),
            creditCardId,
            "Nubank",
            6,
            2026,
            new DateOnly(2026, 6, 25),
            new DateOnly(2026, 7, 5),
            CreditCardStatementStatus.Open,
            300m,
            0m,
            300m,
            Array.Empty<CreditCardStatementInstallmentItemDto>(),
            Array.Empty<CreditCardStatementPaymentItemDto>());

    [Fact]
    public async Task Handle_WhenStatementNotFound_ShouldReturnNull()
    {
        var handler = CreateHandler(statement: null);

        var result = await handler.Handle(
            new ExportCreditCardStatementPdfQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(_renderer.Received);
    }

    [Fact]
    public async Task Handle_WhenStatementBelongsToAnotherCard_ShouldReturnNull()
    {
        var statement = Statement(creditCardId: Guid.NewGuid());
        var handler = CreateHandler(statement);

        // The route card id does not match the statement's owning card.
        var result = await handler.Handle(
            new ExportCreditCardStatementPdfQuery(Guid.NewGuid(), statement.Id),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Null(_renderer.Received);
    }

    [Fact]
    public async Task Handle_WhenStatementMatchesCard_ShouldReturnRenderedPdf()
    {
        var cardId = Guid.NewGuid();
        var statement = Statement(cardId);
        var handler = CreateHandler(statement);

        var result = await handler.Handle(
            new ExportCreditCardStatementPdfQuery(cardId, statement.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(statement, _renderer.Received);
        Assert.Equal(_renderer.Content, result!.Content);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("fatura-nubank-2026-06.pdf", result.FileName);
    }
}

internal sealed class FakeCreditCardStatementPdfRenderer : ICreditCardStatementPdfRenderer
{
    public byte[] Content { get; } = { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
    public CreditCardStatementDetailsDto? Received { get; private set; }

    public byte[] Render(CreditCardStatementDetailsDto statement)
    {
        Received = statement;
        return Content;
    }
}
