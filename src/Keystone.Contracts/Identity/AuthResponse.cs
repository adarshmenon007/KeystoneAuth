namespace Keystone.Contracts.Identity;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string DisplayName);
