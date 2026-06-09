using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class CreditCardRepository : ICreditCardRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CreditCardRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid tenantId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CreditCards
            .AnyAsync(card =>
                card.TenantId == tenantId
                && card.NormalizedName == normalizedName
                && card.Id != excludeId,
                cancellationToken);
    }

    public Task<CreditCard?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.CreditCards
            .SingleOrDefaultAsync(
                card => card.TenantId == tenantId && card.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditCard>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CreditCards
            .Where(card => card.TenantId == tenantId)
            .Where(card => includeArchived || card.IsActive)
            .OrderByDescending(card => card.IsActive)
            .ThenBy(card => card.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        await _dbContext.CreditCards.AddAsync(creditCard, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        _dbContext.CreditCards.Update(creditCard);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
