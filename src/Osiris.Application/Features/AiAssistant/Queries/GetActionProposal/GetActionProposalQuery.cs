using MediatR;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.GetActionProposal;

public sealed record GetActionProposalQuery(Guid Id) : IRequest<AiActionProposalDto?>;
