using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.FinancialAccounts.Commands.ArchiveFinancialAccount;

public sealed record ArchiveFinancialAccountCommand(Guid Id) : IRequest<Result>;
