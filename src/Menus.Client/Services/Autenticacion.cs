using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Menus.Client.Services;

/// <summary>Contrato HTTP de la autenticación del cliente.</summary>
public interface IAutenticacionApi
{
    /// <summary>Inicia sesión y devuelve el token emitido por la API.</summary>
    Task<ResultadoLogin> IniciarSesionAsync(CredencialesLogin credenciales, CancellationToken ct = default);

    /// <summary>Obtiene el perfil básico del usuario autenticado.</summary>
    Task<PerfilUsuario> ObtenerPerfilAsync(CancellationToken ct = default);
}

/// <summary>Adaptador HTTP de los endpoints de autenticación.</summary>
public sealed class AutenticacionApi(HttpClient http) : IAutenticacionApi
{
    /// <inheritdoc/>
    public async Task<ResultadoLogin> IniciarSesionAsync(CredencialesLogin credenciales, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login", credenciales, ct);
        await LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ResultadoLogin>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    /// <inheritdoc/>
    public async Task<PerfilUsuario> ObtenerPerfilAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/auth/perfil", ct);
        await LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PerfilUsuario>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    private static async Task LanzarSiErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var mensaje = $"La API respondió {(int)response.StatusCode}.";
        var contenido = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(contenido))
        {
            try
            {
                using var documento = JsonDocument.Parse(contenido);
                if (documento.RootElement.TryGetProperty("detail", out var detalle) && !string.IsNullOrWhiteSpace(detalle.GetString()))
                {
                    mensaje = detalle.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Se conserva el mensaje seguro cuando la respuesta no es ProblemDetails.
            }
        }

        throw new ApiException(response.StatusCode, mensaje);
    }
}

/// <summary>Credenciales que envía la pantalla de inicio de sesión.</summary>
public sealed class CredencialesLogin
{
    /// <summary>Correo electrónico del usuario.</summary>
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Introduce un correo válido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña del usuario.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Resultado de un inicio de sesión exitoso.</summary>
public sealed record ResultadoLogin(string Token, DateTime Expira, string Rol);

/// <summary>Información de perfil disponible en el contrato actual de la API.</summary>
public sealed record PerfilUsuario(string Email, string Rol, Guid? EntidadId);

/// <summary>Persistencia del token de sesión del navegador.</summary>
public interface IAlmacenDeSesion
{
    /// <summary>Obtiene el token almacenado, si existe.</summary>
    ValueTask<string?> ObtenerTokenAsync();

    /// <summary>Guarda el token de sesión.</summary>
    ValueTask GuardarTokenAsync(string token);

    /// <summary>Elimina el token de sesión.</summary>
    ValueTask EliminarTokenAsync();
}

/// <summary>Almacena el JWT en <c>localStorage</c>.</summary>
public sealed class AlmacenDeSesion(IJSRuntime javascript) : IAlmacenDeSesion
{
    private const string ClaveDelToken = "menus-nutricionales.token";

    /// <inheritdoc/>
    public ValueTask<string?> ObtenerTokenAsync() => javascript.InvokeAsync<string?>("localStorage.getItem", ClaveDelToken);

    /// <inheritdoc/>
    public ValueTask GuardarTokenAsync(string token) => javascript.InvokeVoidAsync("localStorage.setItem", ClaveDelToken, token);

    /// <inheritdoc/>
    public ValueTask EliminarTokenAsync() => javascript.InvokeVoidAsync("localStorage.removeItem", ClaveDelToken);
}

/// <summary>Operaciones de sesión que usan las páginas y el menú.</summary>
public interface ISesionAutenticada
{
    /// <summary>Guarda el token y publica el usuario autenticado.</summary>
    Task IniciarSesionAsync(string token);

    /// <summary>Elimina el token y publica el estado anónimo.</summary>
    Task CerrarSesionAsync();
}

/// <summary>Expone el JWT almacenado como estado de autenticación de Blazor.</summary>
public sealed class ProveedorDeEstadoDeAutenticacion(IAlmacenDeSesion almacen) : AuthenticationStateProvider, ISesionAutenticada
{
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    /// <inheritdoc/>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await almacen.ObtenerTokenAsync();
        return CrearEstado(token);
    }

    /// <summary>Guarda el token y publica el usuario autenticado.</summary>
    public async Task IniciarSesionAsync(string token)
    {
        await almacen.GuardarTokenAsync(token);
        NotifyAuthenticationStateChanged(Task.FromResult(CrearEstado(token)));
    }

    /// <summary>Elimina el token y publica el estado anónimo.</summary>
    public async Task CerrarSesionAsync()
    {
        await almacen.EliminarTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
    }

    private static AuthenticationState CrearEstado(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !IntentarLeerClaims(token, out var claims))
        {
            return Anonimo;
        }

        var expiracion = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (!long.TryParse(expiracion, out var segundos) || DateTimeOffset.FromUnixTimeSeconds(segundos) <= DateTimeOffset.UtcNow)
        {
            return Anonimo;
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", "name", "role")));
    }

    private static bool IntentarLeerClaims(string token, out List<Claim> claims)
    {
        claims = [];
        var partes = token.Split('.');
        if (partes.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var documento = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            foreach (var propiedad in documento.RootElement.EnumerateObject())
            {
                if (propiedad.Value.ValueKind == JsonValueKind.String || propiedad.Value.ValueKind == JsonValueKind.Number)
                {
                    claims.Add(new Claim(propiedad.Name, propiedad.Value.ToString()));
                }
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

/// <summary>Agrega el JWT almacenado a cada llamada saliente de la API.</summary>
public sealed class AutorizacionHandler(IAlmacenDeSesion almacen) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await almacen.ObtenerTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
