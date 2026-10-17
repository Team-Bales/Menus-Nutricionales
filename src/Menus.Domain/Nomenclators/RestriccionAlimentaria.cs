using Menus.Domain.Abstractions;

namespace Menus.Domain.Nomenclators;

/// <summary>
/// Condición dietética que limita el uso de ciertos alimentos en una dieta
/// (p. ej. "Sin gluten", "Hiposódica", "Sin lactosa", "Vegetariana").
/// Una dieta puede tener varias restricciones activas simultáneamente.
/// Durante la generación automática de menús, solo se incluyen alimentos
/// que no infringen ninguna de las restricciones de la dieta objetivo.
/// Se persiste en PostgreSQL con seed data inicial.
/// Los grupos nutricionales que excluye (relación <c>Excluye</c> del MERX) no tienen seed:
/// se cargan manualmente desde la aplicación.
/// </summary>
public sealed class RestriccionAlimentaria : Entity<Guid>
{
    private readonly List<RestriccionGrupoExcluido> _gruposExcluidos = [];

    /// <param name="id">Identificador único de la restricción.</param>
    /// <param name="nombre">Nombre descriptivo de la restricción (no puede estar vacío).</param>
    /// <exception cref="ArgumentException">Si <paramref name="nombre"/> es nulo o solo espacios.</exception>
    public RestriccionAlimentaria(Guid id, string nombre) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre;
    }

    /// <summary>Nombre descriptivo de la restricción alimentaria (p. ej. "Sin gluten").</summary>
    public string Nombre { get; }

    /// <summary>Grupos nutricionales que excluye la restricción (solo lectura).</summary>
    public IReadOnlyCollection<RestriccionGrupoExcluido> GruposExcluidos => _gruposExcluidos.AsReadOnly();

    /// <summary>Registra que la restricción excluye un grupo nutricional.</summary>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional que se excluye.</param>
    /// <returns>La exclusión registrada.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si la restricción ya excluye ese grupo: el par restricción y grupo identifica cada exclusión (R6-05c).
    /// </exception>
    /// <remarks>
    /// Que ninguna dieta exija un grupo que una de sus restricciones excluye (R9-04) se comprueba en la base,
    /// porque involucra dietas, que son otro agregado.
    /// </remarks>
    public RestriccionGrupoExcluido ExcluirGrupo(Guid grupoNutricionalId)
    {
        if (_gruposExcluidos.Any(e => e.GrupoNutricionalId == grupoNutricionalId))
        {
            throw new InvalidOperationException($"La restricción ya excluye el grupo nutricional {grupoNutricionalId}.");
        }

        var exclusion = new RestriccionGrupoExcluido(Id, grupoNutricionalId);
        _gruposExcluidos.Add(exclusion);
        return exclusion;
    }

    /// <summary>Quita la exclusión de un grupo nutricional.</summary>
    /// <param name="grupoNutricionalId">Identificador del grupo nutricional que deja de estar excluido.</param>
    /// <exception cref="InvalidOperationException">Si la restricción no excluye ese grupo.</exception>
    public void QuitarExclusion(Guid grupoNutricionalId)
    {
        var exclusion = _gruposExcluidos.SingleOrDefault(e => e.GrupoNutricionalId == grupoNutricionalId)
            ?? throw new InvalidOperationException($"La restricción no excluye el grupo nutricional {grupoNutricionalId}.");

        _gruposExcluidos.Remove(exclusion);
    }
}
