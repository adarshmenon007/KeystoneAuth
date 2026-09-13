using ErrorOr;
using Keystone.Application.Identity.Common;
using Keystone.Domain.Accounts;

namespace Keystone.Application.Common.Interfaces;

/// <summary>
/// Issues and rotates the access/refresh token pair. Implemented in Infrastructure — it owns
/// JWT signing and the server-side refresh-token table.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>Mints a fresh access JWT and a new refresh-token family. Used by register and login.</summary>
    Task<AuthResult> IssueTokensAsync(AuthenticatedAccount account, CancellationToken ct = default);

    /// <summary>
    /// Validates a presented refresh token and rotates it: the old token is invalidated and a
    /// new access/refresh pair is returned. A token presented after it was already rotated is
    /// treated as reuse and revokes the whole family. Any failure returns
    /// <c>IdentityErrors.InvalidRefreshToken</c>.
    /// </summary>
    Task<ErrorOr<AuthResult>> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes every refresh token belonging to the owner of <paramref name="refreshToken"/>. A no-op if unknown.</summary>
    Task RevokeAllForOwnerAsync(string refreshToken, CancellationToken ct = default);
}
