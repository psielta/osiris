using MediatR;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Features.FinancialAccounts.Queries.GetFinancialAccountForEdit;

public sealed record GetFinancialAccountForEditQuery(Guid Id) : IRequest<FinancialAccountEditDto?>;
