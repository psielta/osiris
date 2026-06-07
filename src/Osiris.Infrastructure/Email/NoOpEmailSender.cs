using Osiris.Application.Common.Interfaces;

namespace Osiris.Infrastructure.Email;

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
