using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.RejectAction;

public sealed record RejectActionCommand(Guid Id) : IRequest<Result>;
