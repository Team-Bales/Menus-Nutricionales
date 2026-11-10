using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Valoraciones.RegistrarValoracion;

/// <summary>
/// Registra la valoración, emitida por el nutricionista actual, sobre la asignación evaluada, e invalida la
/// caché de sus valoraciones más recientes (informe, «R2»; #101).
/// </summary>
internal sealed class RegistrarValoracionHandler(
    IMenuRepository menus,
    IValoracionRepository valoraciones,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    ICache cache) : IHandler<RegistrarValoracionCommand, IdCreadoDto>
{
    /// <exception cref="NoEncontradoException">
    /// Si el menú no existe, o si el paciente no tiene ese menú asignado (no hay nada que evaluar).
    /// </exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarValoracionCommand solicitud, CancellationToken ct)
    {
        var menu = await menus.GetByIdAsync(solicitud.MenuId, ct)
            ?? throw new NoEncontradoException("menú", solicitud.MenuId);

        if (menu.Asignaciones.All(a => a.PacienteId != solicitud.PacienteId))
        {
            throw new NoEncontradoException("asignación", (solicitud.MenuId, solicitud.PacienteId));
        }

        var valoracion = new ValoracionNutricional(
            Guid.NewGuid(),
            solicitud.MenuId,
            solicitud.PacienteId,
            actor.Id,
            solicitud.ResultadoNutricionalId,
            DateTime.UtcNow);

        await valoraciones.AddAsync(valoracion, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        await cache.InvalidarAsync(CacheDeValoracionesRecientes.Clave(solicitud.PacienteId), ct);

        return new IdCreadoDto(valoracion.Id);
    }
}
