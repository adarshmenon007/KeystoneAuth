namespace Keystone.Contracts.Identity;

/// <summary>Used by both <c>POST /auth/refresh</c> and <c>POST /auth/logout</c>.</summary>
public record RefreshRequest(string RefreshToken);
