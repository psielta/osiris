using FluentValidation;

namespace Osiris.Application.Features.AiAssistant.Commands.SendMessage;

public sealed class SendAiMessageCommandValidator : AbstractValidator<SendAiMessageCommand>
{
    // Mirrors AiAgentOptions.MaxMessageCharacters default; the upper bound also caps prompt-injection payloads.
    public const int MaxMessageLength = 4000;

    public SendAiMessageCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty().WithMessage("Digite uma mensagem para o assistente.")
            .MaximumLength(MaxMessageLength)
            .WithMessage($"A mensagem deve ter no máximo {MaxMessageLength} caracteres.");
    }
}
