using Keystone.Contracts.Identity;

namespace Keystone.Web.Api;

/// <summary>Typed client for the protected <c>/me</c> endpoint.</summary>
public sealed class MeApiClient : ApiClientBase
{
    public MeApiClient(HttpClient http) : base(http)
    {
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken ct = default)
    {
        using var response = await Http.GetAsync("me", ct);
        return await ReadAsync<MeResponse>(response, ct);
    }
}
