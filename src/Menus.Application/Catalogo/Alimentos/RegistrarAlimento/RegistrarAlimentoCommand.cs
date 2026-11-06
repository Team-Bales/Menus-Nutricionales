using Menus.Application.Comun;

namespace Menus.Application.Catalogo.Alimentos.RegistrarAlimento;

/// <summary>
/// Registra un alimento nuevo en el banco. Quien lo describe es el actor de la petición (ADR-010, §6), no un campo
/// del comando.
/// </summary>
/// <param name="Nombre">Nombre del alimento: no vacío y sin repetir entre alimentos y platos (RC-01).</param>
/// <param name="GrupoNutricionalId">Grupo nutricional al que pertenece.</param>
/// <param name="TipoPreparacionId">Tipo de preparación, del catálogo (RD-01).</param>
/// <param name="NivelCaloricoId">Nivel calórico, del catálogo (RD-02).</param>
public sealed record RegistrarAlimentoCommand(
    string Nombre,
    Guid GrupoNutricionalId,
    Guid TipoPreparacionId,
    Guid NivelCaloricoId) : ICommand<IdCreadoDto>;
