using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Commands.SendMessage;

/// <summary>
/// Sends a user message to the assistant. When <see cref="ConversationId"/> is null a new conversation
/// is started; otherwise the message continues an existing one owned by the current user.
/// </summary>
public sealed record SendAiMessageCommand(Guid? ConversationId, string Message)
    : IRequest<Result<AiTurnDto>>;
