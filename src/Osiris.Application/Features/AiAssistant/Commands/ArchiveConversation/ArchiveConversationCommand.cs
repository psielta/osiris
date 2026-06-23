using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.ArchiveConversation;

public sealed record ArchiveConversationCommand(Guid Id) : IRequest<Result>;
