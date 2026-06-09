using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCardStatementPayments.Commands.RegisterCreditCardStatementPayment;

public sealed record RegisterCreditCardStatementPaymentCommand(
    Guid StatementId,
    decimal? Amount,
    DateOnly? PaidAt,
    Guid? FinancialAccountId,
    string? Notes) : IRequest<Result<Guid>>;
