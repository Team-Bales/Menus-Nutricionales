using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.GestionDeMenus.ListarConsumosDePaciente;

/// <summary>
/// Junta los servicios de la asignación de cada menú del paciente y les pone nombre a los ítems, buscados en lote.
/// </summary>
/// <remarks>
/// No se pagina, como los menús del paciente (#84): son los servicios de su estancia. Dos menús servidos el mismo
/// día se ordenan por su identificador, para que el orden sea estable.
/// </remarks>
internal sealed class ListarConsumosDePacienteHandler(
    IMenuRepository menus,
    IPacienteRepository pacientes,
    IItemAlimentarioRepository items) : IHandler<ListarConsumosDePacienteQuery, IReadOnlyList<ServicioDto>>
{
    /// <exception cref="NoEncontradoException">Si el paciente no existe.</exception>
    public async Task<IReadOnlyList<ServicioDto>> HandleAsync(ListarConsumosDePacienteQuery solicitud, CancellationToken ct)
    {
        // Sin esto, un paciente que no existe y uno sin servicios darían la misma lista vacía.
        _ = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        var servicios = (await menus.GetByPacienteAsync(solicitud.PacienteId, ct))
            .SelectMany(m => m.Asignaciones
                .Where(a => a.PacienteId == solicitud.PacienteId)
                .SelectMany(a => a.Servicios)
                .Select(s => (Menu: m, Servicio: s)))
            .ToList();

        var nombres = (await items.GetByIdsAsync(
                [.. servicios.SelectMany(x => x.Servicio.Consumos).Select(c => c.ItemId).Distinct()], ct))
            .ToDictionary(i => i.Id, i => i.Nombre);

        return
        [
            .. servicios
                .OrderByDescending(x => x.Servicio.FechaServicio)
                .ThenBy(x => x.Menu.Id)
                .Select(x => new ServicioDto(x.Menu.Id, x.Menu.DietaId, x.Servicio.FechaServicio, Consumos(x.Servicio.Consumos, nombres))),
        ];
    }

    // El ítem existe: el consumo referencia la inclusión, en RESTRICT (RX-01). Si faltara, se omite, como en el detalle
    // del menú.
    private static IReadOnlyList<ConsumoDto> Consumos(
        IEnumerable<Consumo> consumos,
        IReadOnlyDictionary<Guid, string> nombres)
        =>
        [
            .. consumos
                .Where(c => nombres.ContainsKey(c.ItemId))
                .Select(c => new ConsumoDto(c.ItemId, nombres[c.ItemId], c.Aceptacion))
                .OrderBy(c => c.Nombre, StringComparer.CurrentCulture),
        ];
}
