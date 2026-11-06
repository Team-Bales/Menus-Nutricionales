using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Platos.RegistrarPlato;

/// <summary>
/// Registra un plato nuevo con su composición inicial. Quien lo describe es el actor de la petición (ADR-010, §6).
/// </summary>
/// <param name="Nombre">Nombre del plato: no vacío y sin repetir entre alimentos y platos (RC-01).</param>
/// <param name="GrupoNutricionalId">Grupo nutricional al que pertenece.</param>
/// <param name="TipoPreparacionId">Tipo de preparación, del catálogo (RD-01).</param>
/// <param name="NivelCaloricoId">Nivel calórico, del catálogo (RD-02).</param>
/// <param name="Ingredientes">Al menos un alimento simple, sin repetir (RK-03, RS-12).</param>
public sealed record RegistrarPlatoCommand(
    string Nombre,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId,
    IReadOnlyList<IngredienteDelPlato> Ingredientes) : ICommand<IdCreadoDto>;

/// <summary>Un ingrediente que se agrega a un plato.</summary>
/// <param name="AlimentoId">Alimento simple que lo compone.</param>
/// <param name="Cantidad">Cantidad del alimento en el plato (&gt; 0, RD-05).</param>
public sealed record IngredienteDelPlato(Guid AlimentoId, decimal Cantidad);
