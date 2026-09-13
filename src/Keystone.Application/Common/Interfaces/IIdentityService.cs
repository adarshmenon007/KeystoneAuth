using ErrorOr;
using Keystone.Domain.Accounts;

namespace Keystone.Application.Common.Interfaces;

/// <summary>
/// The auth-store seam. The implementation lives in Infrastructure and owns ASP.NET Core
/// Identity; the Application layer never sees Identity types.
/// </summary>
public interface IIdentityService
{
    /// <returns>
    /// The created <see cref="AuthenticatedAccount"/>, or <c>IdentityErrors.EmailAlreadyInUse</c> /
    /// <c>IdentityErrors.WeakPassword</c> / <c>IdentityErrors.RegistrationFailed</c>.
    /// </returns>
    Task<ErrorOr<AuthenticatedAccount>> RegisterAccountAsync(string email, string password, string displayName, CancellationToken ct = default);

    /// <summary>
    /// Verifies the email/password pair, applying ASP.NET Identity's default lockout.
    /// </summary>
    Task<ErrorOr<AuthenticatedAccount>> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
}
