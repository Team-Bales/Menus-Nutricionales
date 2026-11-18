using Menus.Client;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var configuredApiUri)
    ? configuredApiUri.AbsoluteUri
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<ActorHeaderHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ActorHeaderHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
});
builder.Services.AddScoped<ICatalogoApi, CatalogoApi>();

await builder.Build().RunAsync();
