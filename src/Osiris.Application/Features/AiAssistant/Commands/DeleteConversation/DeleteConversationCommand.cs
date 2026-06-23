using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.DeleteConversation;

/// <summary>Permanently deletes a conversation owned by the current user (and all its data).</summary>
public sealed record DeleteConversationCommand(Guid Id) : IRequest<Result>;
