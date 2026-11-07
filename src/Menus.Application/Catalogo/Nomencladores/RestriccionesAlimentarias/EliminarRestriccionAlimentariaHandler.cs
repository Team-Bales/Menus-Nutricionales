using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Elimina la restricción alimentaria si no está en uso.</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; las claves foráneas en <c>RESTRICT</c> que la protegen
/// siguen siendo la última barrera (ADR-010, §3).
/// </remarks>
internal sealed class EliminarRestriccionAlimentariaHandler(
    INomencladorRepository<RestriccionAlimentaria> restricciones,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarRestriccionAlimentariaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la restricción no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si alguna dieta la aplica o ella misma excluye algún grupo.</exception>
    public async Task<Unidad> HandleAsync(EliminarRestriccionAlimentariaCommand solicitud, CancellationToken ct)
    {
        var restriccion = await restricciones.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("restricción alimentaria", solicitud.Id);

        if (await restricciones.EstaEnUsoAsync(restriccion.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"La restricción alimentaria «{restriccion.Nombre}» está en uso: no se puede eliminar.");
        }

        restricciones.Remove(restriccion);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
