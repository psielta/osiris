using Osiris.Application.Common.Interfaces;
using Osiris.Domain.Entities;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class FakeAiFeedbackRepository : IAiFeedbackRepository
{
    private readonly bool _messageExists;

    public FakeAiFeedbackRepository(bool messageExists) => _messageExists = messageExists;

    public List<AiFeedback> Added { get; } = new();

    public Task<bool> MessageExistsForUserAsync(
        Guid tenantId,
        string userId,
        Guid messageId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_messageExists);

    public Task AddAsync(AiFeedback feedback, CancellationToken cancellationToken)
    {
        Added.Add(feedback);
        return Task.CompletedTask;
    }
}
