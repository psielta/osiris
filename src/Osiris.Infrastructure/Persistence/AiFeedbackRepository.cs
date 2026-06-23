using Microsoft.EntityFrameworkCore;
using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Infrastructure.Persistence;

public sealed class AiFeedbackRepository : IAiFeedbackRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AiFeedbackRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> MessageExistsForUserAsync(
        Guid tenantId,
        string userId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.AiMessages.AnyAsync(
            message => message.TenantId == tenantId && message.UserId == userId && message.Id == messageId,
            cancellationToken);
    }

    public async Task AddAsync(AiFeedback feedback, CancellationToken cancellationToken)
    {
        await _dbContext.AiFeedbacks.AddAsync(feedback, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
