using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.CreditCardPurchases.Support;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; init; } = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);
}
