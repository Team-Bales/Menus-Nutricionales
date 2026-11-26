namespace Menus.Application.Comun;

/// <summary>
/// El caso de uso pide un recurso que no existe. La API la traduce a 404 (ADR-010, §4).
/// </summary>
/// <remarks>
/// Es de la capa de aplicación y no del dominio: «no existe» lo sabe el manejador cuando el repositorio devuelve
/// <see langword="null"/>, no una entidad.
/// </remarks>
public sealed class NoEncontradoException : Exception
{
    /// <param name="recurso">Nombre del recurso, en singular (por ejemplo «alimento»).</param>
    /// <param name="id">Identificador buscado.</param>
    public NoEncontradoException(string recurso, object id)
        : base($"No existe el {recurso} {id}.")
    {
        Recurso = recurso;
        Id = id;
    }

    /// <summary>
    /// Crea la excepción con un mensaje propio, para cuando «No existe el {recurso} {id}» no dice lo que falta (por
    /// ejemplo, un alimento que existe pero no tiene ficha nutricional).
    /// </summary>
    /// <param name="recurso">Nombre del recurso, en singular.</param>
    /// <param name="id">Identificador buscado.</param>
    /// <param name="mensaje">Mensaje para el cliente.</param>
    public NoEncontradoException(string recurso, object id, string mensaje)
        : base(mensaje)
    {
        Recurso = recurso;
        Id = id;
    }

    /// <summary>Nombre del recurso buscado.</summary>
    public string Recurso { get; }

    /// <summary>Identificador buscado.</summary>
    public object Id { get; }
}
