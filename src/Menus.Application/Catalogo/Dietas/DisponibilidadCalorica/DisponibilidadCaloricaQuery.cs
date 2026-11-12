using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;

/// <summary>Disponibilidad calórica de una dieta, servida desde Redis cuando está disponible (#101).</summary>
/// <param name="DietaId">Identificador de la dieta.</param>
public sealed record DisponibilidadCaloricaQuery(Guid DietaId) : IQuery<DisponibilidadCaloricaDto>;
