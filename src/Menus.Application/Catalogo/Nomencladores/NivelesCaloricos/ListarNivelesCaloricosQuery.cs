using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.NivelesCaloricos;

/// <summary>
/// Lista completa de los niveles calóricos del catálogo cerrado (RD-02), en el orden del catálogo: bajo, medio y alto.
/// </summary>
public sealed record ListarNivelesCaloricosQuery : IQuery<IReadOnlyList<NomencladorDto>>;
