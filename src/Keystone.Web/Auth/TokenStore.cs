using System.Text.Json;
using Keystone.Contracts.Identity;
using Microsoft.JSInterop;

namespace Keystone.Web.Auth;

/// <summary>
/// Holds the current access/refresh token pair in memory and mirrors it to browser
/// <c>localStorage</c> so a page refresh keeps the session. Scoped: one instance per app load.
/// </summary>
public sealed class TokenStore
{
    private const string StorageKey = "keystone.auth";

    private readonly IJSRuntime _js;
    private Task? _loadTask;

    public TokenStore(IJSRuntime js)
    {
        _js = js;
    }

    public AuthResponse? Current { get; private set; }

    /// <summary>Reads the persisted token on first call; a no-op afterwards.</summary>
    public ValueTask EnsureLoadedAsync() => new(_loadTask ??= LoadAsync());

    private async Task LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
                Current = JsonSerializer.Deserialize<AuthResponse>(json);
        }
        catch (JsonException)
        {
            Current = null;
        }
    }

    public async Task SetAsync(AuthResponse auth)
    {
        Current = auth;
        _loadTask = Task.CompletedTask;
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(auth));
    }

    public async Task ClearAsync()
    {
        Current = null;
        _loadTask = Task.CompletedTask;
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}
