using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.CreditCards.Commands.ArchiveCreditCard;

public sealed record ArchiveCreditCardCommand(Guid Id) : IRequest<Result>;
