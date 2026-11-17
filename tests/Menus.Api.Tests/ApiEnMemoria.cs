using Menus.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Menus.Api.Tests;

/// <summary>
/// La API entera en memoria (<see cref="WebApplicationFactory{TEntryPoint}"/>), con una configuración JWT de prueba, y
/// los tokens que acepta.
/// </summary>
/// <remarks>Ninguna petición de estas pruebas llega a la base: la cadena de conexión solo tiene que existir.</remarks>
internal static class ApiEnMemoria
{
    private const string Clave = "clave-de-prueba-de-al-menos-treinta-y-dos-bytes";
    private const string Emisor = "pruebas";

    /// <summary>Levanta la API en el entorno indicado.</summary>
    /// <param name="entorno"><c>Development</c> o <c>Production</c>.</param>
    public static WebApplicationFactory<Program> Crear(string entorno = "Production") => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment(entorno)
            .UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=sin_uso")
            .UseSetting("Jwt:Key", Clave)
            .UseSetting("Jwt:Issuer", Emisor));

    /// <summary>Un token que la API acepta, firmado con la clave de prueba.</summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="entidadId">Nutricionista o paciente asociado, si lo hay.</param>
    public static string Token(string rol, Guid? entidadId = null) => EmisorCon(Clave).Emitir(Guid.NewGuid(), rol, entidadId).Token;

    /// <summary>Un token con el formato correcto, pero firmado con otra clave: la API lo rechaza.</summary>
    /// <param name="rol">Rol del usuario.</param>
    public static string TokenConOtraClave(string rol)
        => EmisorCon("otra-clave-de-al-menos-treinta-y-dos-bytes-distinta").Emitir(Guid.NewGuid(), rol).Token;

    private static JwtTokenService EmisorCon(string clave)
        => new(Options.Create(new JwtOptions { Key = clave, Issuer = Emisor, ExpiresMinutes = 5 }));
}
