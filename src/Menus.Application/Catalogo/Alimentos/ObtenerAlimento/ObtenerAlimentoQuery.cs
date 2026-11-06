using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Alimentos.ObtenerAlimento;

/// <summary>Detalle de un alimento.</summary>
/// <param name="Id">Identificador del alimento.</param>
public sealed record ObtenerAlimentoQuery(Guid Id) : IQuery<AlimentoDto>;
