using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.Commands.CreateFinancialAccount;

public sealed record CreateFinancialAccountCommand(
    string Name,
    FinancialAccountType? Type,
    decimal? InitialBalance) : IRequest<Result<Guid>>;
