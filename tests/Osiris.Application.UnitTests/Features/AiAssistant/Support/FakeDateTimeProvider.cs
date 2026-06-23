using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.AiAssistant.Support;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; init; } = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
}
