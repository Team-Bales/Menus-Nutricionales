namespace Menus.Domain.Entidades;

/// <summary>
/// Estado de un <see cref="Menu"/>. No se almacena: se deriva de su dictamen (submodelo #7, §4). Sirve para filtrar
/// los listados (#80).
/// </summary>
public enum EstadoDeMenu
{
    /// <summary>Todavía no tiene dictamen (<see cref="Menu.EsPendiente"/>).</summary>
    Pendiente,

    /// <summary>Fue aprobado (<see cref="Menu.EsFinal"/>).</summary>
    Final,

    /// <summary>Fue rechazado (<see cref="Menu.EsRechazado"/>).</summary>
    Rechazado,
}
