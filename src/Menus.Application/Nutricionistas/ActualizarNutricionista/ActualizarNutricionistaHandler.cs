using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Nutricionistas.ActualizarNutricionista;

/// <summary>Actualiza el nombre y la especialidad del nutricionista.</summary>
internal sealed class ActualizarNutricionistaHandler(
    INutricionistaRepository nutricionistas,
    INomencladorRepository<Especialidad> especialidades,
    IUnitOfWork unidadDeTrabajo) : IHandler<ActualizarNutricionistaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista o la especialidad no existen.</exception>
    public async Task<Unidad> HandleAsync(ActualizarNutricionistaCommand solicitud, CancellationToken ct)
    {
        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.Id);

        _ = await especialidades.GetByIdAsync(solicitud.EspecialidadId, ct)
            ?? throw new NoEncontradoException("especialidad", solicitud.EspecialidadId);

        nutricionista.Actualizar(solicitud.Nombre.Trim(), solicitud.EspecialidadId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
