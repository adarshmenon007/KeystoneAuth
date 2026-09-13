using ErrorOr;
using Keystone.Application.Common.Interfaces;
using Keystone.Application.Identity;
using Keystone.Domain.Accounts;
using Microsoft.AspNetCore.Identity;

namespace Keystone.Infrastructure.Identity;

/// <summary>Wraps ASP.NET Core Identity's <see cref="UserManager{TUser}"/> behind the Application seam.</summary>
internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<KeystoneUser> _userManager;

    public IdentityService(UserManager<KeystoneUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ErrorOr<AuthenticatedAccount>> RegisterAccountAsync(
        string email, string password, string displayName, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
            return IdentityErrors.EmailAlreadyInUse;

        var user = new KeystoneUser { Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = displayName };
        IdentityResult created = await _userManager.CreateAsync(user, password);
        if (!created.Succeeded)
            return MapCreationFailure(created);

        return new AuthenticatedAccount(user.Id, user.Email!, user.DisplayName);
    }

    public async Task<ErrorOr<AuthenticatedAccount>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return IdentityErrors.InvalidCredentials;

        if (await _userManager.IsLockedOutAsync(user))
            return IdentityErrors.InvalidCredentials;

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            return IdentityErrors.InvalidCredentials;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return new AuthenticatedAccount(user.Id, user.Email!, user.DisplayName);
    }

    private static Error MapCreationFailure(IdentityResult result)
    {
        var codes = result.Errors.Select(e => e.Code).ToArray();

        if (codes.Any(c => c.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
            return IdentityErrors.EmailAlreadyInUse;

        if (codes.Any(c => c.Contains("Password", StringComparison.OrdinalIgnoreCase)))
            return IdentityErrors.WeakPassword;

        return IdentityErrors.RegistrationFailed;
    }
}
