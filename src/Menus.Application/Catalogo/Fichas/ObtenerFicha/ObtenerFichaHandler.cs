using Menus.Application.Comun;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Catalogo.Fichas.ObtenerFicha;

/// <summary>
/// Devuelve la ficha nutricional de un alimento o un plato (#283). Primero comprueba el ítem en PostgreSQL, para que
/// el 404 distinga «el ítem no existe» de «el ítem no tiene ficha».
/// </summary>
internal sealed class ObtenerFichaHandler(
    IAlimentoRepository alimentos,
    IPlatoRepository platos,
    IAlmacenDeFichas almacen) : IHandler<ObtenerFichaQuery, FichaNutricionalDto>
{
    public async Task<FichaNutricionalDto> HandleAsync(ObtenerFichaQuery solicitud, CancellationToken ct)
    {
        var recurso = ReglasDeLaFicha.Recurso(solicitud.Clase);
        var existe = solicitud.Clase == ClaseDeItem.Plato
            ? await platos.GetByIdAsync(solicitud.ItemId, ct) is not null
            : await alimentos.GetByIdAsync(solicitud.ItemId, ct) is not null;
        if (!existe)
        {
            throw new NoEncontradoException(recurso, solicitud.ItemId);
        }

        return await almacen.ObtenerAsync(solicitud.Clase, solicitud.ItemId, ct)
            ?? throw new NoEncontradoException(
                recurso,
                solicitud.ItemId,
                $"El {recurso} {solicitud.ItemId} no tiene ficha nutricional.");
    }
}
