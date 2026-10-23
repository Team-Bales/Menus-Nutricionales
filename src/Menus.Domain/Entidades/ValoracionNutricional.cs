using Menus.Domain.Abstractions;
using Menus.Domain.Nomenclators;

namespace Menus.Domain.Entidades;

/// <summary>
/// Valoración nutricional de un paciente sobre un menú que le fue asignado.
/// </summary>
/// <remarks>
/// <see cref="FechaValoracion"/> no está en el MERX, que ordena las valoraciones de forma indirecta: por la
/// fecha de creación del menú evaluado y por la cadena revisada → generada de las revalorizaciones
/// (submodelo #7, §4). Se mantiene para cumplir de forma directa con la orientación del proyecto, que pide
/// responder casi al instante a «las valoraciones más recientes» de nutricionistas y pacientes (issue #101),
/// y eso exige ordenarlas por el momento en que se emitieron (decisión del equipo, 16/10).
/// </remarks>
public sealed class ValoracionNutricional : Entity<Guid>
{
    /// <param name="id">Identificador único de la valoración.</param>
    /// <param name="menuId">Identificador del menú de la asignación evaluada.</param>
    /// <param name="pacienteId">Identificador del paciente de la asignación evaluada.</param>
    /// <param name="nutricionistaId">Identificador del nutricionista que emite la valoración.</param>
    /// <param name="resultadoNutricionalId">Identificador de una de las categorías de <see cref="ResultadoNutricional"/>.</param>
    /// <param name="fechaValoracion">Fecha y hora en que se emite la valoración (UTC).</param>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="fechaValoracion"/> no está en UTC o si <paramref name="resultadoNutricionalId"/> no es una
    /// categoría del catálogo (R7-19).
    /// </exception>
    public ValoracionNutricional(
        Guid id,
        Guid menuId,
        Guid pacienteId,
        Guid nutricionistaId,
        Guid resultadoNutricionalId,
        DateTime fechaValoracion)
        : base(id)
    {
        if (fechaValoracion.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("El instante de valoración debe estar en UTC (DateTimeKind.Utc).", nameof(fechaValoracion));
        }

        if (!ResultadoNutricional.Existe(resultadoNutricionalId))
        {
            throw new ArgumentException($"{resultadoNutricionalId} no es una categoría de resultado nutricional.", nameof(resultadoNutricionalId));
        }

        MenuId = menuId;
        PacienteId = pacienteId;
        NutricionistaId = nutricionistaId;
        ResultadoNutricionalId = resultadoNutricionalId;
        FechaValoracion = fechaValoracion;
    }

    /// <summary>FK → Menú de la asignación evaluada.</summary>
    public Guid MenuId { get; }

    /// <summary>FK → Paciente de la asignación evaluada.</summary>
    public Guid PacienteId { get; }

    /// <summary>FK → Nutricionista que emite la valoración.</summary>
    public Guid NutricionistaId { get; }

    /// <summary>FK → Categoría de resultado nutricional.</summary>
    public Guid ResultadoNutricionalId { get; }

    /// <summary>Fecha y hora en que se emitió la valoración, en UTC.</summary>
    public DateTime FechaValoracion { get; }
}
