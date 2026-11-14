using Menus.Infrastructure.Mongo;

namespace Menus.Infrastructure.Mongo;

/// <summary>
/// Resultado de una pasada de conciliación RM-10.
/// </summary>
/// <param name="Revisadas">Número de fichas recorridas en la pasada.</param>
/// <param name="HuerfanasEliminadas">Fichas eliminadas porque su ítem no existe o su clase no coincide.</param>
/// <param name="NombresActualizados">Fichas de plato en las que se actualizó al menos una copia de <c>nombre</c> en <c>ingredientes</c>.</param>
/// <param name="FichasInvalidas">Fichas cuyo <c>origen.id</c> no se pudo parsear a GUID (no se borran).</param>
/// <param name="DivergenciasRm06">Fichas de plato cuyo conjunto de <c>ingredientes.alimentoId</c> difiere de <c>Compone</c> en PostgreSQL (solo si C1 del ADR-009 fue aceptado).</param>
public sealed record ResultadoConciliacion(
    int Revisadas,
    int HuerfanasEliminadas,
    int NombresActualizados,
    int FichasInvalidas,
    int DivergenciasRm06 = 0);
