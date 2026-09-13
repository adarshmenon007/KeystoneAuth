namespace Keystone.Application.Identity.Common;

/// <summary>
/// The token bundle handed back to a client after register / login / refresh. Mirrors
/// <c>Keystone.Contracts.Identity.AuthResponse</c> — the API controller maps between them.
/// </summary>
/// <param name="AccessToken">Short-lived JWT bearer token (~15 min).</param>
/// <param name="RefreshToken">Opaque rotating refresh token (~14 days sliding).</param>
/// <param name="ExpiresAtUtc">When <paramref name="AccessToken"/> stops being valid.</param>
/// <param name="UserId">The authenticated account's id — also carried as the JWT <c>sub</c> claim.</param>
/// <param name="DisplayName">For client-side display without a round-trip.</param>
public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string DisplayName);
