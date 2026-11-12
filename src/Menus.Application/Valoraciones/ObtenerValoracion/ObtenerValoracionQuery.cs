using Menus.Application.Comun;

namespace Menus.Application.Valoraciones.ObtenerValoracion;

/// <summary>Detalle de una valoración, con su resultado expandido.</summary>
/// <param name="Id">Identificador de la valoración.</param>
public sealed record ObtenerValoracionQuery(Guid Id) : IQuery<ValoracionDto>;
