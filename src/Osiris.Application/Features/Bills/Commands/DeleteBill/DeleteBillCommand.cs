using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Bills.Commands.DeleteBill;

public sealed record DeleteBillCommand(Guid Id) : IRequest<Result>;
