using Menus.Domain.Entidades;

namespace Menus.Application.GestionDeMenus;

/// <summary>Un nutricionista que creó o dictaminó un menú, con su nombre.</summary>
/// <param name="Id">Identificador del nutricionista.</param>
/// <param name="Nombre">Nombre del nutricionista.</param>
public sealed record NutricionistaDelMenuDto(Guid Id, string Nombre);

/// <summary>El dictamen de un menú (relación <c>Valida</c>).</summary>
/// <param name="Revisor">Jefe de nutrición que lo dictaminó.</param>
/// <param name="Fecha">Instante del dictamen, en UTC.</param>
/// <param name="Aprobado"><see langword="true"/> si lo aprobó; <see langword="false"/> si lo rechazó.</param>
/// <param name="Observaciones">Señalamientos del revisor, o nulo si no los hay.</param>
public sealed record DictamenDto(NutricionistaDelMenuDto Revisor, DateTime Fecha, bool Aprobado, string? Observaciones);

/// <summary>Un menú sin su parametrización ni sus ítems: la forma que usa el listado (#80; glosario, §3 F1).</summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó (relación <c>Crea</c>).</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado">
/// <c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c> (<see cref="EstadoDeMenu"/>), como texto para que el cliente no
/// dependa del orden del enumerado.
/// </param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza (relación <c>Origina</c>), o nulo.</param>
public sealed record MenuDto(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenuDto Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDto? Dictamen,
    Guid? ReemplazaA)
{
    /// <summary>Copia los datos del menú y les pone nombre al creador y al revisor.</summary>
    /// <param name="menu">Menú del dominio, con su dictamen.</param>
    /// <param name="nombres">Nombre de cada nutricionista, por su identificador (<see cref="NombresDeNutricionistas"/>).</param>
    internal static MenuDto Desde(Menu menu, IReadOnlyDictionary<Guid, string> nombres)
    {
        var dictamen = menu.Validacion is { } v
            ? new DictamenDto(new(v.NutricionistaId, nombres[v.NutricionistaId]), v.FechaValidacion, v.Aprobado, v.Observaciones)
            : null;

        return new MenuDto(
            menu.Id,
            menu.DietaId,
            menu.FechaCreacion,
            new(menu.NutricionistaCreadorId, nombres[menu.NutricionistaCreadorId]),
            menu.EsAutomatico,
            menu.CantidadTotalAlimentos,
            EstadoDe(menu).ToString(),
            dictamen,
            menu.ValidacionOrigenId);
    }

    /// <summary>El estado se deriva del dictamen (submodelo #7, §4).</summary>
    private static EstadoDeMenu EstadoDe(Menu menu) => menu.Validacion switch
    {
        null => EstadoDeMenu.Pendiente,
        { Aprobado: true } => EstadoDeMenu.Final,
        _ => EstadoDeMenu.Rechazado,
    };
}

/// <summary>La proporción de alimentos de un tipo de preparación en un menú.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Nombre">Nombre del tipo de preparación.</param>
/// <param name="Proporcion">Proporción, en (0, 1].</param>
public sealed record ProporcionDelMenuDto(Guid TipoPreparacionId, string Nombre, decimal Proporcion);

/// <summary>Un grupo nutricional que cubre un menú, o un ítem que incluye: identificador y nombre.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Nombre">Nombre.</param>
public sealed record ElementoDelMenuDto(Guid Id, string Nombre);

/// <summary>
/// Un menú con su parametrización y sus ítems, todo con nombre: la forma que usa el detalle (#80; glosario, §3 F1 y
/// F3). Los primeros campos son los de <see cref="MenuDto"/>.
/// </summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó (relación <c>Crea</c>).</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado"><c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>, como en <see cref="MenuDto"/>.</param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza (relación <c>Origina</c>), o nulo.</param>
/// <param name="Distribucion">Proporción por tipo de preparación, en el orden del catálogo.</param>
/// <param name="GruposNutricionales">Grupos que cubre, ordenados por nombre.</param>
/// <param name="Items">Alimentos y platos que incluye, ordenados por nombre.</param>
public sealed record MenuDetalleDto(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenuDto Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDto? Dictamen,
    Guid? ReemplazaA,
    IReadOnlyList<ProporcionDelMenuDto> Distribucion,
    IReadOnlyList<ElementoDelMenuDto> GruposNutricionales,
    IReadOnlyList<ElementoDelMenuDto> Items);
