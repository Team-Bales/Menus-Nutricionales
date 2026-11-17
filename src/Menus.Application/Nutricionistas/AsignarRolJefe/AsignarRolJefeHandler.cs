using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Nutricionistas.AsignarRolJefe;

/// <summary>
/// Designa al nutricionista como jefe de nutrición. Idempotente: si ya lo era, no cambia nada
/// (<see cref="Menus.Domain.Entidades.Nutricionista.DesignarJefe"/>).
/// </summary>
internal sealed class AsignarRolJefeHandler(INutricionistaRepository nutricionistas, IUnitOfWork unidadDeTrabajo)
    : IHandler<AsignarRolJefeCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista no existe.</exception>
    public async Task<Unidad> HandleAsync(AsignarRolJefeCommand solicitud, CancellationToken ct)
    {
        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.NutricionistaId);

        nutricionista.DesignarJefe();
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
