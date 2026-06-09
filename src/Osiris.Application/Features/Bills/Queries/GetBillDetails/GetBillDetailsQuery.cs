using MediatR;
using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Application.Features.Bills.Queries.GetBillDetails;

public sealed record GetBillDetailsQuery(Guid Id) : IRequest<BillDetailsDto?>;
