using MediatR;
using Osiris.Application.Features.FinancialAccounts.DTOs;

namespace Osiris.Application.Features.FinancialAccounts.Queries.ListFinancialAccounts;

public sealed record ListFinancialAccountsQuery(bool IncludeArchived = true)
    : IRequest<IReadOnlyCollection<FinancialAccountListItemDto>>;
