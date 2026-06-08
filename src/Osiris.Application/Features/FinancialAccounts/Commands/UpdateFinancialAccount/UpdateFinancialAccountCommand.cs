using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Domain.Enums;

namespace Osiris.Application.Features.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed record UpdateFinancialAccountCommand(
    Guid Id,
    string Name,
    FinancialAccountType? Type) : IRequest<Result>;
