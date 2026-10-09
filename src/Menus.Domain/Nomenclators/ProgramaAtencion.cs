using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

public sealed class ProgramaAtencion : Entity<Guid>
{
    public string Nombre { get; }

    public ProgramaAtencion(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }
}
