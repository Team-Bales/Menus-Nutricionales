using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Alimentos.RegistrarAlimento;

/// <summary>
/// Registra el alimento, devuelve su identificador e invalida la disponibilidad calórica de todas las dietas
/// (informe, «Consultas recurrentes»; #101): un alimento nuevo puede ser compatible con cualquiera.
/// </summary>
internal sealed class RegistrarAlimentoHandler(
    IItemAlimentarioRepository items,
    IAlimentoRepository alimentos,
    IDietaRepository dietas,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    ICache cache) : IHandler<RegistrarAlimentoCommand, IdCreadoDto>
{
    /// <exception cref="ReglaDeNegocioException">Si ya hay un alimento o un plato con ese nombre (RC-01).</exception>
    public async Task<IdCreadoDto> HandleAsync(RegistrarAlimentoCommand solicitud, CancellationToken ct)
    {
        var nutricionistaId = actor.Id;
        var nombre = solicitud.Nombre.Trim();

        // Se comprueba antes para dar un mensaje claro; el índice único de RC-01 sigue siendo la última barrera
        // (ADR-010, §3), por ejemplo ante dos altas simultáneas.
        if (await items.ExisteNombreAsync(nombre, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe un alimento o un plato llamado «{nombre}».", "RC-01");
        }

        var alimento = new Alimento(
            Guid.NewGuid(),
            nombre,
            solicitud.GrupoNutricionalId,
            solicitud.TipoPreparacionId,
            solicitud.NivelCaloricoId,
            nutricionistaId);

        await alimentos.AddAsync(alimento, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        await CacheDeDisponibilidadCalorica.InvalidarTodasAsync(dietas, cache, ct);

        return new IdCreadoDto(alimento.Id);
    }
}
