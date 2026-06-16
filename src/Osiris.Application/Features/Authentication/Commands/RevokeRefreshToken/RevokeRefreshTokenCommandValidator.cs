using FluentValidation;

namespace Osiris.Application.Features.Authentication.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithMessage("Informe o token de atualização.");
    }
}
