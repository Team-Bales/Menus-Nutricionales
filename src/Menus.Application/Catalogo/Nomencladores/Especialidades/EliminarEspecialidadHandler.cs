using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Elimina la especialidad si no está en uso.</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; la FK en <c>RESTRICT</c> de <c>Nutricionista</c> sigue
/// siendo la última barrera (ADR-010, §3).
/// </remarks>
internal sealed class EliminarEspecialidadHandler(
    INomencladorRepository<Especialidad> especialidades,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarEspecialidadCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la especialidad no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si algún nutricionista la tiene (RR-12).</exception>
    public async Task<Unidad> HandleAsync(EliminarEspecialidadCommand solicitud, CancellationToken ct)
    {
        var especialidad = await especialidades.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("especialidad", solicitud.Id);

        if (await especialidades.EstaEnUsoAsync(especialidad.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"La especialidad «{especialidad.Nombre}» tiene nutricionistas: no se puede eliminar.", "RR-12");
        }

        especialidades.Remove(especialidad);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
