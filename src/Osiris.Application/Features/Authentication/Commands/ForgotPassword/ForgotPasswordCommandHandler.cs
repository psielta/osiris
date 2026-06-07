using MediatR;
using Osiris.Application.Common.Interfaces;
using Osiris.Application.Common.Models;

namespace Osiris.Application.Features.Authentication.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IEmailSender emailSender)
    {
        _identityService = identityService;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenResult = await _identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

        if (tokenResult.IsSuccess && !string.IsNullOrWhiteSpace(tokenResult.Value))
        {
            await _emailSender.SendPasswordResetAsync(request.Email, tokenResult.Value, cancellationToken);
        }

        return Result.Success();
    }
}
