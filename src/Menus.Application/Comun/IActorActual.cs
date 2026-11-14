namespace Menus.Application.Comun;

/// <summary>
/// Quién ejecuta el caso de uso: el nutricionista que describe un ítem, crea un menú o lo dictamina (ADR-010, §6).
/// </summary>
/// <remarks>
/// Los manejadores lo toman de aquí y nunca de un campo del comando, que el cliente podría inventar. La implementación
/// vive en <c>Menus.Api</c> y lo lee del token (#74): el <c>entidad_id</c> de un usuario con rol <c>Nutricionista</c>.
/// </remarks>
public interface IActorActual
{
    /// <summary>
    /// Identificador del nutricionista que actúa.
    /// </summary>
    /// <exception cref="ActorNoIdentificadoException">Si la petición no identifica a nadie.</exception>
    Guid Id { get; }
}

/// <summary>
/// La petición no identifica a quien actúa y el caso de uso lo necesita. La API la traduce a 401.
/// </summary>
public sealed class ActorNoIdentificadoException : Exception
{
    /// <param name="mensaje">Qué falta o qué está mal (por ejemplo, la cabecera ausente).</param>
    public ActorNoIdentificadoException(string mensaje)
        : base(mensaje)
    {
    }
}
