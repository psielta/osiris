using MediatR;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.ListConversationProposals;

/// <summary>Lists the pending write proposals of a conversation owned by the current user.</summary>
public sealed record ListConversationProposalsQuery(Guid ConversationId)
    : IRequest<IReadOnlyCollection<AiActionProposalDto>>;
