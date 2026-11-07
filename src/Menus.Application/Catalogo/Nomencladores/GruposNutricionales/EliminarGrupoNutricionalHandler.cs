using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Elimina el grupo nutricional si no está en uso.</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; las claves foráneas en <c>RESTRICT</c> que lo protegen
/// siguen siendo la última barrera (ADR-010, §3).
/// </remarks>
internal sealed class EliminarGrupoNutricionalHandler(
    INomencladorRepository<GrupoNutricional> grupos,
    IUnitOfWork unidadDeTrabajo) : IHandler<EliminarGrupoNutricionalCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el grupo no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si algo lo referencia: un ítem, una dieta, una restricción o un menú.</exception>
    public async Task<Unidad> HandleAsync(EliminarGrupoNutricionalCommand solicitud, CancellationToken ct)
    {
        var grupo = await grupos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("grupo nutricional", solicitud.Id);

        if (await grupos.EstaEnUsoAsync(grupo.Id, ct))
        {
            throw new ReglaDeNegocioException($"El grupo nutricional «{grupo.Nombre}» está en uso: no se puede eliminar.");
        }

        grupos.Remove(grupo);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
