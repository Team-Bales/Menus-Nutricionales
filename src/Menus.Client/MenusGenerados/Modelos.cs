namespace Menus.Client.MenusGenerados;

/// <summary>Nutricionista que creó o dictaminó un menú, con su nombre.</summary>
public sealed record NutricionistaDelMenu(Guid Id, string Nombre);

/// <summary>Dictamen de un menú: quién lo revisó, cuándo, si lo aprobó y sus observaciones.</summary>
public sealed record DictamenDelMenu(NutricionistaDelMenu Revisor, DateTime Fecha, bool Aprobado, string? Observaciones);

/// <summary>Menú sin su parametrización ni sus ítems: la forma que devuelve el listado.</summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó.</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado">Estado como texto: <c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>.</param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza, o nulo.</param>
public sealed record Menu(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenu Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDelMenu? Dictamen,
    Guid? ReemplazaA);

/// <summary>Proporción de alimentos de un tipo de preparación dentro de un menú.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Nombre">Nombre del tipo de preparación.</param>
/// <param name="Proporcion">Proporción, en (0, 1].</param>
public sealed record ProporcionDelMenu(Guid TipoPreparacionId, string Nombre, decimal Proporcion);

/// <summary>Elemento de un menú (un grupo nutricional o un ítem) con su nombre.</summary>
/// <param name="Id">Identificador del elemento.</param>
/// <param name="Nombre">Nombre del elemento.</param>
public sealed record ElementoDelMenu(Guid Id, string Nombre);

/// <summary>Menú con su parametrización y sus ítems, todo con nombre.</summary>
/// <param name="Id">Identificador del menú.</param>
/// <param name="DietaId">Dieta a la que pertenece.</param>
/// <param name="FechaCreacion">Instante de creación, en UTC.</param>
/// <param name="Creador">Nutricionista que lo creó.</param>
/// <param name="EsAutomatico">Si se generó automáticamente.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de alimentos, parámetro de generación.</param>
/// <param name="Estado">Estado como texto: <c>Pendiente</c>, <c>Final</c> o <c>Rechazado</c>.</param>
/// <param name="Dictamen">El dictamen, o nulo si está pendiente.</param>
/// <param name="ReemplazaA">El menú rechazado al que reemplaza, o nulo.</param>
/// <param name="Distribucion">Proporción por tipo de preparación, en el orden del catálogo.</param>
/// <param name="GruposNutricionales">Grupos que cubre, ordenados por nombre.</param>
/// <param name="Items">Alimentos y platos que incluye, ordenados por nombre.</param>
public sealed record MenuDetalle(
    Guid Id,
    Guid DietaId,
    DateTime FechaCreacion,
    NutricionistaDelMenu Creador,
    bool EsAutomatico,
    int CantidadTotalAlimentos,
    string Estado,
    DictamenDelMenu? Dictamen,
    Guid? ReemplazaA,
    IReadOnlyList<ProporcionDelMenu> Distribucion,
    IReadOnlyList<ElementoDelMenu> GruposNutricionales,
    IReadOnlyList<ElementoDelMenu> Items);

/// <summary>Proporción que se envía al generar un menú.</summary>
/// <param name="TipoPreparacionId">Identificador del tipo de preparación.</param>
/// <param name="Proporcion">Proporción como fracción en (0, 1]; todas deben sumar 1.</param>
public sealed record ProporcionDeMenu(Guid TipoPreparacionId, decimal Proporcion);

/// <summary>Parámetros con los que se pide la generación automática de un menú.</summary>
/// <param name="DietaId">Dieta para la que se genera.</param>
/// <param name="CantidadTotalAlimentos">Cantidad total de ítems, mayor que cero.</param>
/// <param name="Distribucion">Proporción de ítems por tipo de preparación; debe sumar 1.</param>
/// <param name="GruposNutricionalesIds">Grupos nutricionales que debe cubrir el menú; al menos uno.</param>
public sealed record GeneracionDeMenu(
    Guid DietaId,
    int CantidadTotalAlimentos,
    IReadOnlyList<ProporcionDeMenu> Distribucion,
    IReadOnlyList<Guid> GruposNutricionalesIds);
