using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
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
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ResultadoLogin>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
    }

    /// <inheritdoc/>
    public async Task<PerfilUsuario> ObtenerPerfilAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("api/auth/perfil", ct);
        await RespuestasDeApi.LanzarSiErrorAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PerfilUsuario>(cancellationToken: ct)
            ?? throw new ApiException(response.StatusCode, "La API devolvió una respuesta vacía.");
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
/// <remarks>
/// La API también devuelve <c>expira</c> y <c>rol</c>, pero el cliente no los necesita: los dos
/// salen del propio token (claims <c>exp</c> y <c>role</c>), que es la única fuente de verdad.
/// </remarks>
/// <param name="Token">JWT emitido por la API.</param>
public sealed record ResultadoLogin(string Token);

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

/// <summary>
/// Almacena el JWT en <c>localStorage</c> y lo mantiene en memoria para no pasar por la
/// interop de JavaScript en cada petición.
/// </summary>
/// <remarks>
/// <c>localStorage</c> mantiene la sesión al recargar o abrir otra pestaña, a cambio de que un
/// script inyectado (XSS) pueda leer el token. Se acepta para este proyecto; ver ADR-015.
/// </remarks>
public sealed class AlmacenDeSesion(IJSRuntime javascript) : IAlmacenDeSesion
{
    private const string ClaveDelToken = "menus-nutricionales.token";
    private string? _token;
    private bool _cargado;

    /// <inheritdoc/>
    public async ValueTask<string?> ObtenerTokenAsync()
    {
        if (!_cargado)
        {
            _token = await javascript.InvokeAsync<string?>("localStorage.getItem", ClaveDelToken);
            _cargado = true;
        }

        return _token;
    }

    /// <inheritdoc/>
    public async ValueTask GuardarTokenAsync(string token)
    {
        await javascript.InvokeVoidAsync("localStorage.setItem", ClaveDelToken, token);
        _token = token;
        _cargado = true;
    }

    /// <inheritdoc/>
    public async ValueTask EliminarTokenAsync()
    {
        await javascript.InvokeVoidAsync("localStorage.removeItem", ClaveDelToken);
        _token = null;
        _cargado = true;
    }
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
    /// <remarks>Un token caducado o mal formado se borra del almacén y cuenta como anónimo.</remarks>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await almacen.ObtenerTokenAsync();
        var claims = TokenJwt.LeerClaimsVigentes(token);
        if (claims is null)
        {
            if (token is not null)
            {
                await almacen.EliminarTokenAsync();
            }

            return Anonimo;
        }

        return Autenticado(claims);
    }

    /// <summary>Guarda el token y publica el usuario autenticado.</summary>
    public async Task IniciarSesionAsync(string token)
    {
        await almacen.GuardarTokenAsync(token);
        var claims = TokenJwt.LeerClaimsVigentes(token);
        NotifyAuthenticationStateChanged(Task.FromResult(claims is null ? Anonimo : Autenticado(claims)));
    }

    /// <summary>Elimina el token y publica el estado anónimo.</summary>
    public async Task CerrarSesionAsync()
    {
        await almacen.EliminarTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
    }

    private static AuthenticationState Autenticado(IEnumerable<Claim> claims)
        => new(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", "name", "role")));
}

/// <summary>
/// Agrega el JWT vigente a cada llamada saliente de la API y cierra la sesión si la API
/// la rechaza con 401.
/// </summary>
/// <remarks>
/// <see cref="ISesionAutenticada"/> y <see cref="NavigationManager"/> se resuelven en el momento
/// de usarlos y no en el constructor, para no crear una dependencia circular si en el futuro el
/// proveedor de estado necesita el <see cref="HttpClient"/>.
/// </remarks>
public sealed class AutorizacionHandler(IAlmacenDeSesion almacen, IServiceProvider servicios) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await almacen.ObtenerTokenAsync();
        var enviaToken = false;
        if (!string.IsNullOrWhiteSpace(token))
        {
            if (TokenJwt.LeerClaimsVigentes(token) is null)
            {
                // Caducado o mal formado: no se manda y se borra para no arrastrarlo.
                await almacen.EliminarTokenAsync();
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                enviaToken = true;
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Solo cuenta como sesión caducada si se mandó un token: un 401 sin token es, por ejemplo,
        // el de unas credenciales incorrectas en el login, y eso lo muestra la propia pantalla.
        if (enviaToken && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await servicios.GetRequiredService<ISesionAutenticada>().CerrarSesionAsync();
            var navegacion = servicios.GetRequiredService<NavigationManager>();
            navegacion.NavigateTo(RutasDeSesion.LoginVolviendoAqui(navegacion));
        }

        return response;
    }
}
