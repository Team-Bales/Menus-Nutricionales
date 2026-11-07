namespace Menus.Application.Comun;

/// <summary>
/// Identidad del usuario autenticado, obtenida exclusivamente de los claims del token JWT.
/// </summary>
/// <remarks>
/// Implementada en <c>Menus.Api</c> leyendo <c>sub</c>, <c>entidad_id</c> y <c>role</c> del token.
/// No consulta ni enriquece con campos role-specific (especialidad, jefe, etc.).
/// <see cref="IActorActual"/> y <see cref="ActorDesdeCabecera"/> no se tocan; #74 hará esa migración.
/// </remarks>
public interface IIdentidadAutenticada
{
    /// <summary>Identificador del usuario (claim <c>sub</c>).</summary>
    Guid UsuarioId { get; }

    /// <summary>Identificador de la entidad de dominio (nutricionista o paciente), si corresponde (claim <c>entidad_id</c>).</summary>
    Guid? EntidadId { get; }

    /// <summary>Rol del usuario (claim <c>role</c>).</summary>
    string Rol { get; }
}