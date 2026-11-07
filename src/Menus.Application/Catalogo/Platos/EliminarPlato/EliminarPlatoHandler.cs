using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos.EliminarPlato;

/// <summary>Elimina el plato si no está en uso; su composición se borra en cascada (RX-02).</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; la clave foránea en <c>RESTRICT</c> sigue siendo la última
/// barrera (ADR-010, §3).
/// </remarks>
internal sealed class EliminarPlatoHandler(
    IItemAlimentarioRepository items,
    IPlatoRepository platos,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarPlatoCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el plato no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si figura en algún menú (RR-22).</exception>
    public async Task<Unidad> HandleAsync(EliminarPlatoCommand solicitud, CancellationToken ct)
    {
        var plato = await platos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("plato", solicitud.Id);

        if (await items.FiguraEnAlgunMenuAsync(plato.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"El plato «{plato.Nombre}» figura en algún menú, y el historial del menú depende de él: no se puede eliminar.",
                "RR-22");
        }

        platos.Remove(plato);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
