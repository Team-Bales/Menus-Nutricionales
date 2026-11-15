using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.GestionDeMenus.RegistrarServicio;

/// <summary>Registra el servicio y sus consumos en el agregado del menú, y confirma una sola vez.</summary>
/// <remarks>
/// <para>
/// Las reglas son del agregado (<c>Menu.RegistrarServicio</c> y <c>Menu.RegistrarConsumo</c>): que el menú sea
/// final, que esté asignado al paciente, que no se le haya servido ya ese día (RC-17), que el día no sea futuro ni
/// anterior a la aprobación (RS-14, respaldada en la base), que cada ítem esté en el menú (RX-01, clave foránea hacia
/// la inclusión) y que no se repita en el servicio (RC-14). Si una falla, no se guarda nada.
/// </para>
/// <para>
/// Lo registra cualquier nutricionista: es un registro clínico, y la autorización para la dieta delimita crear y
/// validar menús (RF-06, RF-07), no servirlos. Eso lo decide la API.
/// </para>
/// </remarks>
internal sealed class RegistrarServicioHandler(
    IMenuRepository menus,
    IPacienteRepository pacientes,
    IUnitOfWork unidadDeTrabajo) : IHandler<RegistrarServicioCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el menú o el paciente no existen.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el menú no es final, si no está asignado al paciente, si ya se le sirvió ese día, si un ítem no está en el
    /// menú o si se repite.
    /// </exception>
    /// <exception cref="DatoInvalidoException">Si el día es futuro o anterior a la aprobación del menú.</exception>
    public async Task<Unidad> HandleAsync(RegistrarServicioCommand solicitud, CancellationToken ct)
    {
        var menu = await menus.GetByIdAsync(solicitud.MenuId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuId);

        // Sin esto, un paciente que no existe daría el mismo 409 que uno al que no se le asignó el menú.
        _ = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        menu.RegistrarServicio(solicitud.PacienteId, solicitud.Fecha);
        foreach (var consumo in solicitud.Consumos)
        {
            menu.RegistrarConsumo(solicitud.PacienteId, solicitud.Fecha, consumo.ItemId, consumo.Aceptacion);
        }

        await unidadDeTrabajo.CommitAsync(ct);
        return Unidad.Valor;
    }
}
