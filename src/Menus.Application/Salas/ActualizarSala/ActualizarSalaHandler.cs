using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Salas.ActualizarSala;

/// <summary>Actualiza el nombre de la sala, con nombre único (#72).</summary>
internal sealed class ActualizarSalaHandler(ISalaRepository salas, IUnitOfWork unidadDeTrabajo)
    : IHandler<ActualizarSalaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si la sala no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si ya existe otra sala con ese nombre.</exception>
    public async Task<Unidad> HandleAsync(ActualizarSalaCommand solicitud, CancellationToken ct)
    {
        var sala = await salas.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("sala", solicitud.Id);

        var nombre = solicitud.Nombre.Trim();

        if (await salas.ExisteNombreAsync(nombre, solicitud.Id, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe una sala llamada «{nombre}».");
        }

        sala.Actualizar(nombre);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
