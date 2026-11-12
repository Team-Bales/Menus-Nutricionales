using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.GestionDeMenus.RegistrarReemplazo;

/// <summary>Vincula el menú de reemplazo con el rechazado.</summary>
/// <remarks>
/// Las reglas del vínculo las comprueba el dominio (<c>Menu.RegistrarComoReemplazo</c>). Que un rechazo origine a lo
/// sumo un menú (RK-15) abarca a otros menús: lo garantiza la base con una clave única, y responde 409.
/// </remarks>
internal sealed class RegistrarReemplazoHandler(IMenuRepository menus, IUnitOfWork unidadDeTrabajo)
    : IHandler<RegistrarReemplazoCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si alguno de los dos menús no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">
    /// Si el menú de reemplazo ya tiene dictamen o ya reemplaza a otro, si se indica a sí mismo (RS-19), si el otro no
    /// fue rechazado (RS-17) o si es de otra dieta (RS-18).
    /// </exception>
    public async Task<Unidad> HandleAsync(RegistrarReemplazoCommand solicitud, CancellationToken ct)
    {
        var menu = await menus.GetByIdAsync(solicitud.MenuId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuId);
        var rechazado = await menus.GetByIdAsync(solicitud.MenuRechazadoId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuRechazadoId);

        menu.RegistrarComoReemplazo(rechazado);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
