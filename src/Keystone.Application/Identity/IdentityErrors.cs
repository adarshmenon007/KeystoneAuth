using ErrorOr;

namespace Keystone.Application.Identity;

/// <summary>
/// Named, reachable-via-valid-input auth failures (project convention — no inline <c>Error.*</c>).
/// </summary>
public static class IdentityErrors
{
    public static Error EmailAlreadyInUse =>
        Error.Conflict("Identity.EmailAlreadyInUse", "An account with this email already exists.");

    public static Error WeakPassword =>
        Error.Validation("Identity.WeakPassword", "Password must be at least 8 characters.");

    public static Error DisplayNameRequired =>
        Error.Validation("Identity.DisplayNameRequired", "Display name is required.");

    public static Error EmailRequired =>
        Error.Validation("Identity.EmailRequired", "A valid email address is required.");

    public static Error InvalidCredentials =>
        Error.Unauthorized("Identity.InvalidCredentials", "Email or password is incorrect.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("Identity.InvalidRefreshToken", "The refresh token is invalid, expired, or has been revoked.");

    public static Error RegistrationFailed =>
        Error.Validation("Identity.RegistrationFailed", "The account could not be created.");
}
