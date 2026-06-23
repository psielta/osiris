using MediatR;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.AiAssistant.Commands.SubmitFeedback;

/// <summary>Records the user's feedback on an assistant message. Rating is normalized to -1/0/+1.</summary>
public sealed record SubmitFeedbackCommand(Guid MessageId, int Rating, string? ReasonCode, string? Comment)
    : IRequest<Result>;
