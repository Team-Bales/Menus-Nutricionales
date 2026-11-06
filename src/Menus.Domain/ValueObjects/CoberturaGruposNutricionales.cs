namespace Menus.Domain.ValueObjects;

/// <summary>
/// Grupos nutricionales que cubre un menú.
/// Al menos uno (RK-09) y sin repetidos (RC-15).
/// </summary>
/// <param name="GrupoIds">Identificadores de los grupos nutricionales cubiertos.</param>
public sealed record CoberturaGruposNutricionales(Guid[] GrupoIds) : IEquatable<CoberturaGruposNutricionales>
{
    /// <summary>
    /// Crea una nueva cobertura validando las reglas de negocio.
    /// </summary>
    /// <param name="grupoIds">Identificadores de los grupos nutricionales.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="grupoIds"/> es nulo.</exception>
    /// <exception cref="ArgumentException">Si la colección está vacía o hay grupos repetidos.</exception>
    public static CoberturaGruposNutricionales Crear(IEnumerable<Guid> grupoIds)
    {
        ArgumentNullException.ThrowIfNull(grupoIds);

        var lista = grupoIds.ToArray();

        if (lista.Length == 0)
        {
            throw new ArgumentException("El menú debe cubrir al menos un grupo nutricional.", nameof(grupoIds));
        }

        var vistos = new HashSet<Guid>();
        foreach (var id in lista)
        {
            if (!vistos.Add(id))
            {
                throw new ArgumentException($"El grupo nutricional {id} aparece más de una vez.", nameof(grupoIds));
            }
        }

        return new CoberturaGruposNutricionales(lista);
    }

    /// <summary>
    /// Obtiene los identificadores como colección de solo lectura.
    /// </summary>
    public IReadOnlyCollection<Guid> AsReadOnly() => GrupoIds;

    /// <inheritdoc/>
    public bool Equals(CoberturaGruposNutricionales? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GrupoIds.Length != other.GrupoIds.Length)
        {
            return false;
        }

        return new HashSet<Guid>(GrupoIds).SetEquals(other.GrupoIds);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Hash code independiente del orden
        var hash = new HashCode();
        foreach (var id in GrupoIds.OrderBy(x => x))
        {
            hash.Add(id);
        }
        return hash.ToHashCode();
    }
}
