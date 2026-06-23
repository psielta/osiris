using Osiris.Application.Common.Models;
using Osiris.Application.Features.AiAssistant.Commands.SubmitFeedback;
using Osiris.Application.UnitTests.Features.AiAssistant.Support;

namespace Osiris.Application.UnitTests.Features.AiAssistant;

public sealed class SubmitFeedbackCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Records_feedback_when_message_belongs_to_user()
    {
        var repo = new FakeAiFeedbackRepository(messageExists: true);
        var handler = new SubmitFeedbackCommandHandler(repo, new FakeCurrentUser(_tenantId));

        var result = await handler.Handle(
            new SubmitFeedbackCommand(Guid.NewGuid(), 1, "helpful", "Boa resposta"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var feedback = Assert.Single(repo.Added);
        Assert.Equal(1, feedback.Rating);
        Assert.Equal(_tenantId, feedback.TenantId);
    }

    [Fact]
    public async Task Returns_not_found_when_message_does_not_belong_to_user()
    {
        var repo = new FakeAiFeedbackRepository(messageExists: false);
        var handler = new SubmitFeedbackCommandHandler(repo, new FakeCurrentUser(_tenantId));

        var result = await handler.Handle(
            new SubmitFeedbackCommand(Guid.NewGuid(), -1, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ResultErrorCodes.NotFound);
        Assert.Empty(repo.Added);
    }
}
