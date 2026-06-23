using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface IAiFeedbackRepository
{
    /// <summary>True when the message exists and belongs to the given tenant + user.</summary>
    Task<bool> MessageExistsForUserAsync(Guid tenantId, string userId, Guid messageId, CancellationToken cancellationToken);

    Task AddAsync(AiFeedback feedback, CancellationToken cancellationToken);
}
