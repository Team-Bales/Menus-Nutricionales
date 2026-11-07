using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.RestriccionesAlimentarias;

/// <summary>Registra la restricción alimentaria y devuelve su identificador.</summary>
internal sealed class RegistrarRestriccionAlimentariaHandler(
    INomencladorRepository<RestriccionAlimentaria> restricciones,
    IUnitOfWork unidadDeTrabajo) : IHandler<RegistrarRestriccionAlimentariaCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya existe una restricción con ese nombre (RD-08).</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarRestriccionAlimentariaCommand solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();

        if (await restricciones.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe una restricción alimentaria llamada «{nombre}».", "RD-08");
        }

        var restriccion = new RestriccionAlimentaria(Guid.NewGuid(), nombre);
        await restricciones.AddAsync(restriccion, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(restriccion.Id);
    }
}
