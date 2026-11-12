using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores;

/// <summary>Un nomenclador, abierto o de catálogo cerrado, tal como lo ve el cliente (ADR-010, §2).</summary>
/// <param name="Id">Identificador del nomenclador.</param>
/// <param name="Nombre">Nombre descriptivo.</param>
public sealed record NomencladorDto(Guid Id, string Nombre)
{
    internal static NomencladorDto Desde(GrupoNutricional grupo) => new(grupo.Id, grupo.Nombre);

    internal static NomencladorDto Desde(RestriccionAlimentaria restriccion) => new(restriccion.Id, restriccion.Nombre);

    internal static NomencladorDto Desde(ProgramaAtencion programa) => new(programa.Id, programa.Nombre);

    internal static NomencladorDto Desde(Especialidad especialidad) => new(especialidad.Id, especialidad.Nombre);

    internal static NomencladorDto Desde(TipoPreparacion tipo) => new(tipo.Id, tipo.Nombre);

    internal static NomencladorDto Desde(NivelCalorico nivel) => new(nivel.Id, nivel.Nombre);
}
