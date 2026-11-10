using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Nomencladores.TiposPreparacion;

/// <summary>Lista completa de los tipos de preparación del catálogo cerrado (RD-01), en el orden del catálogo.</summary>
public sealed record ListarTiposPreparacionQuery : IQuery<IReadOnlyList<NomencladorDto>>;
