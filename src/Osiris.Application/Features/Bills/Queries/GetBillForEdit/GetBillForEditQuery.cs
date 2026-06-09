using MediatR;
using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Application.Features.Bills.Queries.GetBillForEdit;

public sealed record GetBillForEditQuery(Guid Id) : IRequest<BillEditDto?>;
