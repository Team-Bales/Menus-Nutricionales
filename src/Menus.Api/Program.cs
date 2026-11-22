using System.Text.Json;
using Menus.Api.Actor;
using Menus.Api.Auth;
using Menus.Api.Cors;
using Menus.Api.Errores;
using Menus.Api.Seguridad;
using Menus.Application;
using Menus.Application.Comun;
using Menus.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using IPNetwork = System.Net.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Capa de aplicación (ADR-010): controladores, errores como ProblemDetails y quién actúa.
// Sin el [Required] implícito de los tipos no anulables, un campo vacío o ausente llega a FluentValidation y
// responde 422 con el formato del ADR-010 (§3), en lugar de un 400 de MVC con otro formato.
builder.Services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Entrada malformada (JSON roto, cuerpo JSON vacío, tipo incorrecto): 400 con un mensaje genérico. No se reutiliza el
// ProblemDetails de MVC porque detalla el error de enlace y el nombre del tipo, información que no hace falta
// exponer (§13.3, OWASP). La validación de negocio sigue siendo 422 (ADR-010, §3).
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        // Se arma con ProblemDetailsFactory para que el 400 lleve title, type y traceId, como el resto de errores.
        var problema = contexto.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(
                contexto.HttpContext,
                StatusCodes.Status400BadRequest,
                detail: "La solicitud no es válida.");

        // ContentResult y no ObjectResult: el [Produces("application/json")] de los controladores impondría
        // application/json, y el contrato de errores del ADR-010 (§4) es application/problem+json.
        return new ContentResult
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json",
            Content = JsonSerializer.Serialize(problema, JsonSerializerOptions.Web),
        };
    };
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeExcepciones>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorActual, ActorDesdeToken>();
builder.Services.AddScoped<IIdentidadAutenticada, IdentidadAutenticada>();
builder.Services.AddHostedService<AuthBootstrapService>();
builder.Services.AddPoliticasDeAutorizacion();
builder.Services.AddCorsDelCliente(builder.Configuration);

// Solo configura HstsOptions; la cabecera HSTS la emite el middleware de cabeceras de seguridad, no UseHsts.
builder.Services.AddHsts(opciones =>
{
    opciones.MaxAge = TimeSpan.FromDays(365);
    opciones.IncludeSubDomains = true;
    opciones.Preload = false;
});

// La API está detrás de nginx en Compose. Con estas cabeceras, el rate limiting ve la IP del cliente y HSTS el
// esquema original. Solo se aceptan proxies de redes privadas: si la API se expone directamente, hay que quitar
// estas redes o limitarlas a la del proxy.
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.ForwardLimit = 1;
    opciones.KnownIPNetworks.Add(IPNetwork.Parse("10.0.0.0/8"));
    opciones.KnownIPNetworks.Add(IPNetwork.Parse("172.16.0.0/12"));
    opciones.KnownIPNetworks.Add(IPNetwork.Parse("192.168.0.0/16"));
});

builder.Services.AddOptions<OpcionesDeLimiteDePeticiones>()
    .Bind(builder.Configuration.GetSection(OpcionesDeLimiteDePeticiones.Seccion))
    .Validate(o => o.PermitLimit > 0, "RateLimiting:PermitLimit debe ser mayor que cero.")
    .Validate(o => o.WindowSeconds > 0, "RateLimiting:WindowSeconds debe ser mayor que cero.")
    .ValidateOnStart();
builder.Services.AddLimiteDePeticiones();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Primero las cabeceras reenviadas: HSTS y el límite de peticiones dependen del esquema y de la IP del cliente.
app.UseForwardedHeaders();

// Las cabeceras se registran en OnStarting para que también las lleven los errores de UseExceptionHandler.
app.UseCabecerasDeSeguridad();

// El navegador tiene que poder leer también los errores (404, 409, 422), y la comprobación previa (OPTIONS) no puede
// llegar a la redirección a HTTPS.
app.UseCors(CorsDelCliente.Politica);

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // El documento OpenAPI de desarrollo no pide token: la política de reserva lo cerraría.
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Después de la autorización: una petición sin permiso no consume cupo del límite.
app.UseRateLimiter();

app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada público para permitir WebApplicationFactory en pruebas de integración.
/// </summary>
public partial class Program { }
