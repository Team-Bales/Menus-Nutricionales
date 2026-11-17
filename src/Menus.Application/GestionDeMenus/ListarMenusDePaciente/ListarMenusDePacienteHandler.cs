using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.GestionDeMenus.ListarMenusDePaciente;

/// <summary>
/// Devuelve el detalle de cada menú asignado al paciente (<see cref="DetallesDeMenus"/>): lo que el paciente necesita
/// ver es qué incluye cada menú, no solo que existe.
/// </summary>
/// <remarks>
/// No se pagina: los menús de un paciente son los de su estancia, pocos comparados con los de una dieta.
/// </remarks>
internal sealed class ListarMenusDePacienteHandler(
    IMenuRepository menus,
    IPacienteRepository pacientes,
    INutricionistaRepository nutricionistas,
    INomencladorRepository<GrupoNutricional> grupos,
    IItemAlimentarioRepository items) : IHandler<ListarMenusDePacienteQuery, IReadOnlyList<MenuDetalleDto>>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe.</exception>
    public async Task<IReadOnlyList<MenuDetalleDto>> HandleAsync(ListarMenusDePacienteQuery solicitud, CancellationToken ct)
    {
        // Sin esto, un paciente que no existe y uno sin menús darían la misma lista vacía.
        _ = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        var asignados = await menus.GetByPacienteAsync(solicitud.PacienteId, ct);
        return await new DetallesDeMenus(nutricionistas, grupos, items).ArmarAsync(asignados, ct);
    }
}
