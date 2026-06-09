using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Bills.Commands.MarkBillAsPending;

public sealed record MarkBillAsPendingCommand(Guid Id) : IRequest<Result>;
