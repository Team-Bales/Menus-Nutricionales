using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.ProgramasAtencion;

/// <summary>Registra el programa de atención y devuelve su identificador.</summary>
internal sealed class RegistrarProgramaAtencionHandler(
    INomencladorRepository<ProgramaAtencion> programas,
    IUnitOfWork unidadDeTrabajo) : IHandler<RegistrarProgramaAtencionCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya existe un programa con ese nombre (RD-08).</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarProgramaAtencionCommand solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();

        if (await programas.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe un programa de atención llamado «{nombre}».", "RD-08");
        }

        var programa = new ProgramaAtencion(Guid.NewGuid(), nombre);
        await programas.AddAsync(programa, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(programa.Id);
    }
}
