using MediatR;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.ListConversations;

/// <summary>Lists the current user's conversations, most recently updated first.</summary>
public sealed record ListConversationsQuery
    : IRequest<IReadOnlyCollection<AiConversationListItemDto>>;
