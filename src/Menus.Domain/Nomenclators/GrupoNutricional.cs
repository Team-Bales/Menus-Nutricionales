using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

public sealed class GrupoNutricional : Entity<Guid>
{
    public string Nombre { get; }

    public GrupoNutricional(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }
}
