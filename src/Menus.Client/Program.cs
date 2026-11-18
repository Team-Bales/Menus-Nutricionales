using Menus.Client;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var configuredApiUri)
    ? configuredApiUri.AbsoluteUri
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAlmacenDeSesion, AlmacenDeSesion>();
builder.Services.AddScoped<ProveedorDeEstadoDeAutenticacion>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ProveedorDeEstadoDeAutenticacion>());
builder.Services.AddScoped<ISesionAutenticada>(sp => sp.GetRequiredService<ProveedorDeEstadoDeAutenticacion>());
builder.Services.AddScoped<AutorizacionHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AutorizacionHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
});
builder.Services.AddScoped<ICatalogoApi, CatalogoApi>();
builder.Services.AddScoped<IAutenticacionApi, AutenticacionApi>();

await builder.Build().RunAsync();
