using Keystone.Web;
using Keystone.Web.Api;
using Keystone.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["KeystoneApi:BaseAddress"];
if (string.IsNullOrWhiteSpace(apiBaseAddress))
{
    throw new InvalidOperationException("Configuration value 'KeystoneApi:BaseAddress' is missing.");
}

var apiBaseUri = new Uri(apiBaseAddress);

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<KeystoneAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<KeystoneAuthenticationStateProvider>());
builder.Services.AddScoped<BearerTokenHandler>();

// Establishes the session — no bearer/refresh handler.
builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = apiBaseUri);

// Protected endpoints — the bearer handler attaches the token and does refresh-on-401.
builder.Services.AddHttpClient<MeApiClient>(client => client.BaseAddress = apiBaseUri)
    .AddHttpMessageHandler<BearerTokenHandler>();

await builder.Build().RunAsync();
