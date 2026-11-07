using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Catalogo.Nomencladores.GruposNutricionales;

/// <summary>Registra el grupo nutricional y devuelve su identificador.</summary>
internal sealed class RegistrarGrupoNutricionalHandler(
    INomencladorRepository<GrupoNutricional> grupos,
    IUnitOfWork unidadDeTrabajo) : IHandler<RegistrarGrupoNutricionalCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya existe un grupo con ese nombre (RD-08).</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarGrupoNutricionalCommand solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();

        // Se comprueba antes para dar un mensaje claro; el índice único de RD-08 sigue siendo la última barrera.
        if (await grupos.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe un grupo nutricional llamado «{nombre}».", "RD-08");
        }

        var grupo = new GrupoNutricional(Guid.NewGuid(), nombre);
        await grupos.AddAsync(grupo, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(grupo.Id);
    }
}
