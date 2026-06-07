using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.Categories.Support;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; init; } = new(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc);
}
