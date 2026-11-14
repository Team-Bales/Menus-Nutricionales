using Menus.Domain.Entidades;

namespace Menus.Application.Nutricionistas;

/// <summary>Un nutricionista, sin sus dietas autorizadas: la forma que usa el listado (#71).</summary>
/// <param name="Id">Identificador del nutricionista.</param>
/// <param name="Nombre">Nombre del nutricionista.</param>
/// <param name="EspecialidadId">Especialidad del nutricionista.</param>
/// <param name="EsJefe">Si ocupa el cargo de jefe de nutrición.</param>
public sealed record NutricionistaDto(Guid Id, string Nombre, Guid EspecialidadId, bool EsJefe)
{
    internal static NutricionistaDto Desde(Nutricionista nutricionista)
        => new(nutricionista.Id, nutricionista.Nombre, nutricionista.EspecialidadId, nutricionista.EsJefe);
}

/// <summary>Una dieta para la que el nutricionista está autorizado (relación <c>Autoriza</c>).</summary>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record DietaAutorizadaDto(Guid DietaId);

/// <summary>
/// Un nutricionista con sus dietas autorizadas: la forma que usa el detalle (#71). Los primeros campos son los de
/// <see cref="NutricionistaDto"/>.
/// </summary>
/// <param name="Id">Identificador del nutricionista.</param>
/// <param name="Nombre">Nombre del nutricionista.</param>
/// <param name="EspecialidadId">Especialidad del nutricionista.</param>
/// <param name="EsJefe">Si ocupa el cargo de jefe de nutrición.</param>
/// <param name="DietasAutorizadas">Dietas para las que está autorizado.</param>
public sealed record NutricionistaDetalleDto(Guid Id, string Nombre, Guid EspecialidadId, bool EsJefe, IReadOnlyList<DietaAutorizadaDto> DietasAutorizadas)
{
    internal static NutricionistaDetalleDto Desde(Nutricionista nutricionista) => new(
        nutricionista.Id,
        nutricionista.Nombre,
        nutricionista.EspecialidadId,
        nutricionista.EsJefe,
        [.. nutricionista.DietasAutorizadas.Select(a => new DietaAutorizadaDto(a.DietaId))]);
}
