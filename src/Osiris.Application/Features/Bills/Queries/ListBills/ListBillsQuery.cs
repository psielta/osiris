using MediatR;
using Osiris.Application.Features.Bills.DTOs;

namespace Osiris.Application.Features.Bills.Queries.ListBills;

public sealed record ListBillsQuery(int Year, int Month) : IRequest<IReadOnlyCollection<BillListItemDto>>;
