using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.GestionDeMenus.ValidarMenu;

/// <summary>Registra el dictamen en nombre del actor, que tiene que ser jefe y estar autorizado para la dieta.</summary>
/// <remarks>
/// La base comprueba lo mismo con triggers (RS-03, RS-05, RS-06, RS-15). Aquí se comprueba antes, para responder con
/// un mensaje claro y sin llegar a la base.
/// </remarks>
internal sealed class ValidarMenuHandler(
    IMenuRepository menus,
    INutricionistaRepository nutricionistas,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor) : IHandler<ValidarMenuCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el menú no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el actor no es jefe de nutrición o no está autorizado para la dieta del menú (RS-03), si es quien creó el
    /// menú (RS-05) o si el menú ya tiene dictamen (RS-15).
    /// </exception>
    public async Task<Unidad> HandleAsync(ValidarMenuCommand solicitud, CancellationToken ct)
    {
        var revisorId = actor.Id;
        var menu = await menus.GetByIdAsync(solicitud.MenuId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuId);

        // RS-03: las dos condiciones, en la misma operación que registra el dictamen.
        var revisor = await nutricionistas.GetByIdAsync(revisorId, ct);
        if (revisor is not { EsJefe: true })
        {
            throw new ReglaDeNegocioException("Solo un jefe de nutrición dictamina un menú.", "RS-03");
        }

        if (!revisor.DietasAutorizadas.Any(a => a.DietaId == menu.DietaId))
        {
            throw new ReglaDeNegocioException(
                "El revisor no está autorizado para la dieta del menú.", "RS-03");
        }

        var observaciones = string.IsNullOrWhiteSpace(solicitud.Observaciones) ? null : solicitud.Observaciones.Trim();
        var ahora = DateTime.UtcNow;

        if (solicitud.Aprobado)
        {
            menu.Aprobar(revisorId, ahora, observaciones);
        }
        else
        {
            menu.Rechazar(revisorId, ahora, observaciones);
        }

        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
