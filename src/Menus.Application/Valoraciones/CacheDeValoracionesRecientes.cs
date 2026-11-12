using Menus.Application.Comun;

namespace Menus.Application.Valoraciones;

/// <summary>
/// Clave de caché de las valoraciones más recientes de un paciente, y cómo invalidarla (informe, «Consultas
/// recurrentes»; #101). La usan tanto <see cref="ListarValoracionesPorPaciente.ListarValoracionesPorPacienteHandler"/>
/// como <see cref="RegistrarValoracion.RegistrarValoracionHandler"/>, que registra una nueva.
/// </summary>
internal static class CacheDeValoracionesRecientes
{
    /// <summary>Tiempo de vida de la clave, como respaldo de la invalidación por evento.</summary>
    public static readonly TimeSpan TiempoDeVida = TimeSpan.FromHours(1);

    /// <summary>Clave de caché de las valoraciones más recientes de un paciente.</summary>
    /// <param name="pacienteId">Identificador del paciente.</param>
    public static string Clave(Guid pacienteId) => $"paciente:{pacienteId}:valoraciones-recientes";
}
