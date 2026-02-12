namespace BuildingBlocks.Abstractions.Contracts;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
