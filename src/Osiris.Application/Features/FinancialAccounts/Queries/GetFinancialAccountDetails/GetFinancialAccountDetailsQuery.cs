using MediatR;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountDetails;

public sealed record GetFinancialAccountDetailsQuery(Guid Id) : IRequest<FinancialAccountStatementDto?>;
