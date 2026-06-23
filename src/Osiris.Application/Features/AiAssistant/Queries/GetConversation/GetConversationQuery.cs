using MediatR;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Queries.GetConversation;

/// <summary>Returns one conversation (owned by the current user) with its user/assistant messages.</summary>
public sealed record GetConversationQuery(Guid Id) : IRequest<AiConversationDetailDto?>;
