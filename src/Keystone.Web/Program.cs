using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Keystone.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Task 15 will add authorization, authentication state provider, and HTTP client setup here

await builder.Build().RunAsync();
