using Osiris.Application.Common.Interfaces;

namespace Osiris.Application.UnitTests.Features.Authentication.Support;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
