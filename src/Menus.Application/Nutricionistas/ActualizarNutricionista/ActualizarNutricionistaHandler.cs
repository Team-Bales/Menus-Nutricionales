using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Nutricionistas.ActualizarNutricionista;

/// <summary>Actualiza el nombre y la especialidad del nutricionista.</summary>
internal sealed class ActualizarNutricionistaHandler(INutricionistaRepository nutricionistas, IUnitOfWork unidadDeTrabajo)
    : IHandler<ActualizarNutricionistaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista no existe.</exception>
    public async Task<Unidad> HandleAsync(ActualizarNutricionistaCommand solicitud, CancellationToken ct)
    {
        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.Id);

        nutricionista.Actualizar(solicitud.Nombre.Trim(), solicitud.EspecialidadId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
