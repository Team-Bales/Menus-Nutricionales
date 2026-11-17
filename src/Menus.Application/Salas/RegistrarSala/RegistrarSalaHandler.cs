using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Salas.RegistrarSala;

/// <summary>Registra la sala, con nombre único (#72).</summary>
internal sealed class RegistrarSalaHandler(ISalaRepository salas, IUnitOfWork unidadDeTrabajo)
    : IHandler<RegistrarSalaCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya existe una sala con ese nombre.</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarSalaCommand solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();

        if (await salas.ExisteNombreAsync(nombre, cancellationToken: ct))
        {
            throw new ReglaDeNegocioException($"Ya existe una sala llamada «{nombre}».");
        }

        var sala = new Sala(Guid.NewGuid(), nombre);
        await salas.AddAsync(sala, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(sala.Id);
    }
}
