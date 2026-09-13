using System.Net;
using System.Net.Http.Json;

namespace Keystone.Web.Api;

/// <summary>
/// Thrown when a Keystone.Api call returns a non-success status code. Carries the status
/// code and, when the body is a ProblemDetails response (the shape ApiControllerBase
/// produces), its detail/title so a page can show a useful message instead of a raw stack trace.
/// </summary>
public sealed class KeystoneApiException : Exception
{
    public KeystoneApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }

    internal static async Task<KeystoneApiException> FromResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string? detail = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            detail = string.IsNullOrWhiteSpace(problem?.Detail) ? problem?.Title : problem.Detail;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Body wasn't ProblemDetails-shaped — fall back to the status line below.
        }

        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
            : $"{(int)response.StatusCode} {response.ReasonPhrase}: {detail}";

        return new KeystoneApiException(response.StatusCode, message);
    }

    private sealed record ProblemDetails(string? Title, string? Detail);
}
