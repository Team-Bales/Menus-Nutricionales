using Menus.Application.Comun;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Dietas.AgregarGrupoDieta;

/// <summary>
/// Agrega un grupo nutricional a la cobertura de una dieta. Si la dieta ya cubre ese grupo, no hace nada
/// (idempotente): no falla ni duplica la cobertura.
/// </summary>
/// <param name="DietaId">Identificador de la dieta.</param>
/// <param name="GrupoNutricionalId">Identificador del grupo nutricional.</param>
/// <param name="Tipo">Requerido o restringido (RD-16).</param>
public sealed record AgregarGrupoDietaCommand(Guid DietaId, Guid GrupoNutricionalId, TipoCobertura Tipo) : ICommand<Unidad>;
