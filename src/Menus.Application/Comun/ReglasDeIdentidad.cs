using Menus.Domain;

namespace Menus.Application.Comun;

/// <summary>
/// Quién puede ver un dato de un paciente, según la identidad del token: el personal ve todo, y un paciente, solo lo
/// suyo (#316).
/// </summary>
/// <remarks>
/// Es para los casos de uso en los que la API no puede decidir con una política, porque el identificador de la ruta no
/// es el del paciente (una valoración, una revalorización) y hay que buscar el dato para saber de quién es. Quién es
/// personal lo define <see cref="RolesUsuario.Personal"/>, el mismo criterio que usan las políticas de la API.
/// </remarks>
internal static class ReglasDeIdentidad
{
    /// <summary>Indica si la identidad es del personal (nutricionista o administrador).</summary>
    /// <param name="identidad">Identidad del token.</param>
    /// <returns><see langword="true"/> si su rol está en <see cref="RolesUsuario.Personal"/>.</returns>
    public static bool EsPersonal(this IIdentidadAutenticada identidad) => RolesUsuario.EsPersonal(identidad.Rol);

    /// <summary>Indica si la identidad es la del paciente <paramref name="pacienteId"/>.</summary>
    /// <remarks>
    /// Exige además el rol <see cref="RolesUsuario.Paciente"/>, como la política <c>PacientePropioOPersonal</c>: el
    /// <c>entidad_id</c> de un nutricionista es el de su ficha, no el de un paciente. Un paciente sin
    /// <c>entidad_id</c> no es el dueño de nada.
    /// </remarks>
    /// <param name="identidad">Identidad del token.</param>
    /// <param name="pacienteId">Paciente dueño del dato.</param>
    /// <returns><see langword="true"/> si es un paciente y su <c>entidad_id</c> es <paramref name="pacienteId"/>.</returns>
    public static bool EsElPaciente(this IIdentidadAutenticada identidad, Guid pacienteId)
        => identidad.Rol == RolesUsuario.Paciente && identidad.EntidadId == pacienteId;

    /// <summary>Indica si la identidad puede ver un dato del paciente <paramref name="pacienteId"/>.</summary>
    /// <param name="identidad">Identidad del token.</param>
    /// <param name="pacienteId">Paciente dueño del dato.</param>
    /// <returns><see langword="true"/> si es del personal o es ese mismo paciente.</returns>
    public static bool EsPersonalOElPaciente(this IIdentidadAutenticada identidad, Guid pacienteId)
        => identidad.EsPersonal() || identidad.EsElPaciente(pacienteId);
}
