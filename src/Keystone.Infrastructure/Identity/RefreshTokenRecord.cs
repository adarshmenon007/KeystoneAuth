namespace Keystone.Infrastructure.Identity;

/// <summary>
/// A single server-side refresh token. Not a domain entity — an Infrastructure persistence
/// concern only. Only the SHA-256 hash of the opaque token is stored, never the token itself.
///
/// Rotation: <c>/auth/refresh</c> marks the presented row <see cref="RevokedAt"/> + sets
/// <see cref="ReplacedByTokenHash"/>, and inserts a successor sharing <see cref="FamilyId"/>.
/// Presenting an already-rotated (or revoked) token is treated as reuse and revokes the
/// whole family.
/// </summary>
internal sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }

    /// <summary>The owning <c>KeystoneUser.Id</c>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Base64 SHA-256 of the opaque token string.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Shared by every token produced by rotating one original login token.</summary>
    public Guid FamilyId { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
