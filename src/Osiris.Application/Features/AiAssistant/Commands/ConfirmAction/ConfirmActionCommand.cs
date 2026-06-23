using MediatR;
using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.DTOs;

namespace Osiris.Application.Features.AiAssistant.Commands.ConfirmAction;

/// <summary>Confirms a pending proposal, executing the underlying financial command exactly once.</summary>
public sealed record ConfirmActionCommand(Guid Id) : IRequest<Result<AiActionResultDto>>;
