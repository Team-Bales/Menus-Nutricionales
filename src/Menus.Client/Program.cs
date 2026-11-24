using Menus.Client;
using Menus.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Punto de entrada del cliente Blazor WebAssembly: registra los componentes raíz y los servicios.
// Son sentencias de nivel superior, así que no llevan comentarios de documentación (///).
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Dirección de la API: Api:BaseUrl de wwwroot/appsettings.json si es una URL absoluta; si no, el mismo origen
// desde el que se sirve el cliente.
var configuredApiBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseUrl = Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var configuredApiUri)
    ? configuredApiUri.AbsoluteUri
    : builder.HostEnvironment.BaseAddress;

// Sesión: el token se guarda en localStorage, y el mismo proveedor publica el estado de autenticación para
// [Authorize] y AuthorizeView, y la sesión para iniciarla y cerrarla.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAlmacenDeSesion, AlmacenDeSesion>();
builder.Services.AddScoped<ProveedorDeEstadoDeAutenticacion>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ProveedorDeEstadoDeAutenticacion>());
builder.Services.AddScoped<ISesionAutenticada>(sp => sp.GetRequiredService<ProveedorDeEstadoDeAutenticacion>());
// HttpClient hacia la API: AutorizacionHandler le agrega el token a cada pedido.
builder.Services.AddScoped<AutorizacionHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AutorizacionHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
});
// Clientes tipados de la API que usan las páginas.
builder.Services.AddScoped<ICatalogoApi, CatalogoApi>();
builder.Services.AddScoped<IAdministracionApi, AdministracionApi>();
builder.Services.AddScoped<IAutenticacionApi, AutenticacionApi>();

await builder.Build().RunAsync();
