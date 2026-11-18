using Menus.Client.Services;

namespace Menus.Client.Tests.Services;

internal sealed class AlmacenDeSesionFalso(string? token = null) : IAlmacenDeSesion
{
    public string? Token { get; private set; } = token;

    public ValueTask<string?> ObtenerTokenAsync() => ValueTask.FromResult(Token);

    public ValueTask GuardarTokenAsync(string token)
    {
        Token = token;
        return ValueTask.CompletedTask;
    }

    public ValueTask EliminarTokenAsync()
    {
        Token = null;
        return ValueTask.CompletedTask;
    }
}

internal sealed class SesionAutenticadaFalsa : ISesionAutenticada
{
    public string? TokenIniciado { get; private set; }
    public bool SeCerro { get; private set; }

    public Task IniciarSesionAsync(string token)
    {
        TokenIniciado = token;
        return Task.CompletedTask;
    }

    public Task CerrarSesionAsync()
    {
        SeCerro = true;
        return Task.CompletedTask;
    }
}

internal sealed class AutenticacionApiFalsa : IAutenticacionApi
{
    public ResultadoLogin? ResultadoDeLogin { get; set; }
    public PerfilUsuario? Perfil { get; set; }
    public Exception? ErrorDeLogin { get; set; }
    public Exception? ErrorDePerfil { get; set; }
    public CredencialesLogin? CredencialesRecibidas { get; private set; }

    public Task<ResultadoLogin> IniciarSesionAsync(CredencialesLogin credenciales, CancellationToken ct = default)
    {
        CredencialesRecibidas = credenciales;
        if (ErrorDeLogin is not null)
        {
            return Task.FromException<ResultadoLogin>(ErrorDeLogin);
        }

        return Task.FromResult(ResultadoDeLogin ?? throw new InvalidOperationException("Configura el resultado del inicio de sesión."));
    }

    public Task<PerfilUsuario> ObtenerPerfilAsync(CancellationToken ct = default)
    {
        if (ErrorDePerfil is not null)
        {
            return Task.FromException<PerfilUsuario>(ErrorDePerfil);
        }

        return Task.FromResult(Perfil ?? throw new InvalidOperationException("Configura el perfil."));
    }
}
