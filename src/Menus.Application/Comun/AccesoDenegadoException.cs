namespace Menus.Application.Comun;

/// <summary>
/// El actor está autenticado pero no tiene permiso para la operación o el recurso pedidos. La API la traduce a 403.
/// </summary>
/// <remarks>
/// Se distingue de <see cref="ActorNoIdentificadoException"/> (401, nadie identificado) y de las reglas del dominio
/// (409, una invariante del agregado): esta es una decisión de autorización de la capa de aplicación, por ejemplo
/// que un nutricionista solo pueda ver su propio perfil (#71).
/// </remarks>
public sealed class AccesoDenegadoException : Exception
{
    /// <param name="mensaje">Por qué se deniega el acceso.</param>
    public AccesoDenegadoException(string mensaje)
        : base(mensaje)
    {
    }
}
