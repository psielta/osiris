namespace Osiris.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken);
}
