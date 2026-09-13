using System.Security.Claims;
using Keystone.Contracts.Identity;
using Keystone.Web.Api;
using Microsoft.AspNetCore.Components.Authorization;

namespace Keystone.Web.Auth;

/// <summary>
/// Bridges the stored JWT (<see cref="TokenStore"/>) to Blazor's auth pipeline. Claims are
/// synthesised from the <see cref="AuthResponse"/> fields the API returns — the browser does
/// not validate the token, the API does.
///
/// <see cref="TryRefreshAsync"/> is the single point through which the access token is
/// renewed (on load when near expiry, and by <see cref="BearerTokenHandler"/> on a 401). A
/// <see cref="SemaphoreSlim"/> serialises it so overlapping callers can't each present the
/// same rotating refresh token and trip the server's reuse-detection.
/// </summary>
public sealed class KeystoneAuthenticationStateProvider : AuthenticationStateProvider
{
    public const string AuthenticationType = "keystone-jwt";

    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly TokenStore _tokenStore;
    private readonly AuthApiClient _authApi;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public KeystoneAuthenticationStateProvider(TokenStore tokenStore, AuthApiClient authApi)
    {
        _tokenStore = tokenStore;
        _authApi = authApi;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _tokenStore.EnsureLoadedAsync();

        var auth = _tokenStore.Current;
        if (auth is null)
            return Anonymous;

        if (IsNearExpiry(auth) && !await TryRefreshAsync())
            return Anonymous;

        auth = _tokenStore.Current;
        return auth is null ? Anonymous : new AuthenticationState(BuildPrincipal(auth));
    }

    /// <summary>
    /// Renews the access token from the stored refresh token, at most once at a time.
    /// Returns true if the session is (still) valid afterwards.
    /// </summary>
    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        await _tokenStore.EnsureLoadedAsync();
        if (_tokenStore.Current is not { } before)
            return false;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_tokenStore.Current is not { } current)
                return false;
            if (!ReferenceEquals(current, before)
                && current.RefreshToken != before.RefreshToken
                && !IsNearExpiry(current))
            {
                return true;
            }

            try
            {
                var refreshed = await _authApi.RefreshAsync(current.RefreshToken, cancellationToken);
                await _tokenStore.SetAsync(refreshed);
                NotifyAuthenticationStateChanged(
                    Task.FromResult(new AuthenticationState(BuildPrincipal(refreshed))));
                return true;
            }
            catch (KeystoneApiException)
            {
                await _tokenStore.ClearAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
                return false;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Called after a successful register / login.</summary>
    public async Task SignInAsync(AuthResponse auth)
    {
        await _tokenStore.SetAsync(auth);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(auth))));
    }

    /// <summary>Clears local state. The caller is responsible for revoking the token server-side.</summary>
    public async Task SignOutAsync()
    {
        await _tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static bool IsNearExpiry(AuthResponse auth) =>
        auth.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddSeconds(15);

    private static ClaimsPrincipal BuildPrincipal(AuthResponse auth)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, auth.DisplayName),
                new Claim(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            },
            AuthenticationType);

        return new ClaimsPrincipal(identity);
    }
}
