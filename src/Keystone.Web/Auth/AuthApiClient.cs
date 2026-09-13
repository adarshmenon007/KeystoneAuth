using System.Net.Http.Json;
using Keystone.Contracts.Identity;
using Keystone.Web.Api;

namespace Keystone.Web.Auth;

/// <summary>
/// Typed client for the <c>/auth</c> endpoints. Deliberately uses a bare <see cref="HttpClient"/>
/// with no bearer/refresh handler — these calls establish the session, so they must not
/// trigger the refresh-on-401 path.
/// </summary>
public sealed class AuthApiClient : ApiClientBase
{
    public AuthApiClient(HttpClient http) : base(http)
    {
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        using var response = await Http.PostAsJsonAsync("auth/register", request, ct);
        return await ReadAsync<AuthResponse>(response, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        using var response = await Http.PostAsJsonAsync("auth/login", request, ct);
        return await ReadAsync<AuthResponse>(response, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        using var response = await Http.PostAsJsonAsync("auth/refresh", new RefreshRequest(refreshToken), ct);
        return await ReadAsync<AuthResponse>(response, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        using var response = await Http.PostAsJsonAsync("auth/logout", new RefreshRequest(refreshToken), ct);
        await EnsureSuccessAsync(response, ct);
    }
}
