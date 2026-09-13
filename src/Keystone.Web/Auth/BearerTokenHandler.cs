using System.Net;
using System.Net.Http.Headers;

namespace Keystone.Web.Auth;

/// <summary>
/// Attaches <c>Authorization: Bearer</c> to outgoing API calls and, on a <c>401</c>, does a
/// single refresh-and-retry. The refresh itself is delegated to
/// <see cref="KeystoneAuthenticationStateProvider.TryRefreshAsync"/>, which serialises it so
/// overlapping 401s don't each rotate the refresh token. Wired onto typed clients that call
/// protected endpoints only — never onto <see cref="AuthApiClient"/>.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;
    private readonly KeystoneAuthenticationStateProvider _authState;

    public BearerTokenHandler(TokenStore tokenStore, KeystoneAuthenticationStateProvider authState)
    {
        _tokenStore = tokenStore;
        _authState = authState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _tokenStore.EnsureLoadedAsync();

        Attach(request, _tokenStore.Current?.AccessToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || _tokenStore.Current is null)
            return response;

        if (!await _authState.TryRefreshAsync(cancellationToken))
            return response;

        response.Dispose();

        var retry = await CloneAsync(request);
        Attach(retry, _tokenStore.Current?.AccessToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void Attach(HttpRequestMessage request, string? accessToken)
    {
        request.Headers.Authorization = string.IsNullOrEmpty(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
        {
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
