using Menus.Application.Comun;

namespace Menus.Api.Tests.Controladores;

/// <summary>Guarda la solicitud recibida y devuelve el resultado preparado.</summary>
internal sealed class DespachadorFalso : IDespachador
{
    public object? Resultado { get; set; }

    public object? Recibida { get; private set; }

    public Task<TResultado> EnviarAsync<TResultado>(ISolicitud<TResultado> solicitud, CancellationToken ct = default)
    {
        Recibida = solicitud;
        return Task.FromResult((TResultado)Resultado!);
    }
}
