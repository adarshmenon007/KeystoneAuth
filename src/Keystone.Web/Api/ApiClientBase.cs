using System.Net.Http.Json;

namespace Keystone.Web.Api;

/// <summary>
/// Base for the typed Keystone.Api clients. Pages depend on a typed client, never on
/// <see cref="HttpClient"/> directly, so the HTTP/auth-token pattern lives in one place.
/// </summary>
public abstract class ApiClientBase
{
    protected ApiClientBase(HttpClient http)
    {
        Http = http;
    }

    protected HttpClient Http { get; }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await KeystoneApiException.FromResponseAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
               ?? throw new KeystoneApiException(
                   response.StatusCode,
                   $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} returned "
                   + $"{(int)response.StatusCode} with an empty body.");
    }

    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await KeystoneApiException.FromResponseAsync(response, cancellationToken);
        }
    }
}
