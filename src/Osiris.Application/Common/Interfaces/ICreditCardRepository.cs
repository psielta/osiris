using Osiris.Domain.Entities;

namespace Osiris.Application.Common.Interfaces;

public interface ICreditCardRepository
{
    Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task<CreditCard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditCard>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken);

    Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken);

    Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken);
}
