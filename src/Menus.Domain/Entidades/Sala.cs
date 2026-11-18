using Menus.Domain.Abstractions;
using Menus.Domain.Excepciones;

namespace Menus.Domain.Entidades;

/// <summary>
/// Representa una sala o unidad asistencial a la que pertenecen los pacientes
/// (relación "Pertenece").
/// </summary>
public sealed class Sala : Entity<Guid>
{
    /// <param name="id">Identificador único de la sala.</param>
    /// <param name="nombre">Nombre descriptivo de la sala (no puede estar vacío).</param>
    /// <exception cref="DatoInvalidoException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public Sala(Guid id, string nombre) : base(id)
    {
        DatoInvalidoException.SiVacio(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo de la sala.</summary>
    public string Nombre { get; private set; }

    /// <summary>Actualiza el nombre de la sala (#72).</summary>
    /// <param name="nombre">Nuevo nombre.</param>
    /// <exception cref="DatoInvalidoException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public void Actualizar(string nombre)
    {
        DatoInvalidoException.SiVacio(nombre);
        Nombre = nombre;
    }
}
