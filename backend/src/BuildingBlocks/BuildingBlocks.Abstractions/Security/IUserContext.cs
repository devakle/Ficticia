namespace BuildingBlocks.Abstractions.Security;

public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
}
