using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

public sealed class Especialidad : Entity<Guid>
{
    public string Nombre { get; }

    public Especialidad(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }
}
