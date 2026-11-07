using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.Especialidades;

/// <summary>Registra la especialidad y devuelve su identificador.</summary>
internal sealed class RegistrarEspecialidadHandler(
    INomencladorRepository<Especialidad> especialidades,
    IUnitOfWork unidadDeTrabajo) : IHandler<RegistrarEspecialidadCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya existe una especialidad con ese nombre (RD-08).</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarEspecialidadCommand solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();

        if (await especialidades.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe una especialidad llamada «{nombre}».", "RD-08");
        }

        var especialidad = new Especialidad(Guid.NewGuid(), nombre);
        await especialidades.AddAsync(especialidad, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(especialidad.Id);
    }
}
