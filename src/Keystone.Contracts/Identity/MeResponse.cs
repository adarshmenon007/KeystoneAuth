namespace Keystone.Contracts.Identity;

/// <summary>The claims-derived shape returned by the protected <c>GET /me</c> endpoint.</summary>
public record MeResponse(Guid UserId, string Email, string DisplayName);
