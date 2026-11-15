using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.GestionDeMenus.AsignarMenu;

/// <summary>
/// Comprueba quién asigna y a quién, y registra la asignación en el agregado del menú.
/// </summary>
/// <remarks>
/// <para>
/// Lo que depende de otros agregados lo comprueba aquí, antes de tocar el menú: que el nutricionista esté autorizado
/// para la dieta del menú, como quien lo crea (RF-07: la autorización delimita lo que puede hacer con los menús de la
/// dieta), y que el paciente esté inscrito en ella (RS-04). La base respalda RS-04 con <c>trg_r6_23</c>.
/// </para>
/// <para>
/// Lo propio del menú lo comprueba el dominio en <c>Menu.AsignarPaciente</c>: que sea final (RS-16, respaldado por
/// <c>trg_r7_34</c>) y que no esté asignado ya a ese paciente (RC-13, clave primaria de la asignación).
/// </para>
/// </remarks>
internal sealed class AsignarMenuHandler(
    IMenuRepository menus,
    IPacienteRepository pacientes,
    INutricionistaRepository nutricionistas,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<AsignarMenuCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el menú o el paciente no existen.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el nutricionista no está autorizado para la dieta del menú, si el paciente no está inscrito en ella (RS-04),
    /// si el menú no es final (RS-16) o si ya está asignado a ese paciente (RC-13).
    /// </exception>
    public async Task<Unidad> HandleAsync(AsignarMenuCommand solicitud, CancellationToken ct)
    {
        var menu = await menus.GetByIdAsync(solicitud.MenuId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuId);

        var nutricionista = await nutricionistas.GetByIdAsync(actor.Id, ct);
        if (nutricionista is null || nutricionista.DietasAutorizadas.All(a => a.DietaId != menu.DietaId))
        {
            throw new ReglaDeNegocioException(
                $"El nutricionista {actor.Id} no está autorizado para la dieta del menú, así que no puede asignarlo.");
        }

        var paciente = await pacientes.GetByIdAsync(solicitud.PacienteId, ct)
            ?? throw new NoEncontradoException("paciente", solicitud.PacienteId);

        if (paciente.Dietas.All(d => d.DietaId != menu.DietaId))
        {
            throw new ReglaDeNegocioException(
                $"El paciente {paciente.Id} no está inscrito en la dieta del menú: solo recibe menús de sus dietas.",
                "RS-04");
        }

        menu.AsignarPaciente(paciente.Id);
        await unidadDeTrabajo.CommitAsync(ct);
        return Unidad.Valor;
    }
}
