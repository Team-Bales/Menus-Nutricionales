using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.ValueObjects;

/// <summary>
/// Distribución de alimentos por tipo de preparación en un menú.
/// Al menos una proporción (RK-09), sin tipos repetidos (RC-11), cada una en (0, 1] (RD-11),
/// y la suma debe ser 1 con tolerancia <see cref="ToleranciaSuma"/> (RS-08).
/// </summary>
/// <param name="Proporciones">Colección de proporciones por tipo de preparación.</param>
public sealed record DistribucionTiposPreparacion(ProporcionTipoPreparacion[] Proporciones) : IEquatable<DistribucionTiposPreparacion>
{
    /// <summary>
    /// Tolerancia admitida para la suma de las proporciones (igual que <see cref="Entidades.Menu.ToleranciaSumaProporciones"/>).
    /// </summary>
    public static readonly decimal ToleranciaSuma = 0.001m;

    /// <summary>
    /// Crea una nueva distribución validando las reglas de negocio.
    /// </summary>
    /// <param name="proporciones">Colección de proporciones por tipo de preparación.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="proporciones"/> es nulo.</exception>
    /// <exception cref="DatoInvalidoException">
    /// Si la colección está vacía, hay tipos de preparación repetidos, o las proporciones no suman 1.
    /// </exception>
    public static DistribucionTiposPreparacion Crear(IEnumerable<ProporcionTipoPreparacion> proporciones)
    {
        ArgumentNullException.ThrowIfNull(proporciones);

        var lista = proporciones.ToArray();

        if (lista.Length == 0)
        {
            throw new DatoInvalidoException(nameof(proporciones), "El menú debe tener al menos una proporción por tipo de preparación.");
        }

        var tiposVistos = new HashSet<Guid>();
        foreach (var p in lista)
        {
            if (!tiposVistos.Add(p.TipoPreparacionId))
            {
                throw new DatoInvalidoException(nameof(proporciones), $"El tipo de preparación {p.TipoPreparacionId} aparece más de una vez en la distribución.");
            }
        }

        var suma = lista.Sum(p => p.Valor);
        if (Math.Abs(suma - 1m) >= ToleranciaSuma)
        {
            throw new DatoInvalidoException(nameof(proporciones), "Las proporciones de la distribución deben sumar 1.");
        }

        return new DistribucionTiposPreparacion(lista);
    }

    /// <summary>
    /// Crea una nueva distribución a partir de tuplas (tipoId, valor), validando las reglas de negocio.
    /// </summary>
    /// <param name="proporciones">Tuplas de (tipoPreparacionId, valor).</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="proporciones"/> es nulo.</exception>
    /// <exception cref="DatoInvalidoException">
    /// Si la colección está vacía, hay tipos de preparación repetidos, o las proporciones no suman 1.
    /// </exception>
    /// <exception cref="DatoInvalidoException">Si algún valor está fuera de (0, 1].</exception>
    /// <exception cref="DatoInvalidoException">Si algún tipo de preparación no existe en el catálogo.</exception>
    public static DistribucionTiposPreparacion CrearDesdeTuplas(IEnumerable<(Guid TipoPreparacionId, decimal Valor)> proporciones)
    {
        ArgumentNullException.ThrowIfNull(proporciones);

        var lista = new List<ProporcionTipoPreparacion>();
        foreach (var (tipoId, valor) in proporciones)
        {
            lista.Add(ProporcionTipoPreparacion.Crear(tipoId, valor));
        }

        return Crear(lista);
    }

    /// <summary>
    /// Obtiene las proporciones como colección de solo lectura.
    /// </summary>
    public IReadOnlyCollection<ProporcionTipoPreparacion> AsReadOnly() => Proporciones;

    /// <inheritdoc/>
    public bool Equals(DistribucionTiposPreparacion? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Proporciones.Length != other.Proporciones.Length)
        {
            return false;
        }

        var thisDict = Proporciones.ToDictionary(p => p.TipoPreparacionId, p => p.Valor);
        var otherDict = other.Proporciones.ToDictionary(p => p.TipoPreparacionId, p => p.Valor);
        return thisDict.OrderBy(kvp => kvp.Key).SequenceEqual(otherDict.OrderBy(kvp => kvp.Key));
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var p in Proporciones.OrderBy(x => x.TipoPreparacionId))
        {
            hash.Add(p.TipoPreparacionId);
            hash.Add(p.Valor);
        }
        return hash.ToHashCode();
    }
}
