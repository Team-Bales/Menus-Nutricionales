using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

public sealed class RestriccionAlimentaria : Entity<Guid>
{
    public string Nombre { get; }

    public RestriccionAlimentaria(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }
}
