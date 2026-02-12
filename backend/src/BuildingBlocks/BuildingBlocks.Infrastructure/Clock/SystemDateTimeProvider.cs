using BuildingBlocks.Abstractions.Contracts;

namespace BuildingBlocks.Infrastructure.Clock;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
