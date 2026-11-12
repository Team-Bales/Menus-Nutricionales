using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;
using Menus.Application.Valoraciones;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Revalorizaciones.RegistrarResultadoRevalorizacion;

/// <summary>Manejador de <see cref="RegistrarResultadoRevalorizacionCommand"/>.</summary>
internal sealed class RegistrarResultadoRevalorizacionHandler(
    IRevalorizacionRepository revalorizaciones,
    IValoracionRepository valoraciones,
    IUnitOfWork unidadDeTrabajo,
    IActorActual actor,
    ICache cache) : IHandler<RegistrarResultadoRevalorizacionCommand, IdCreadoDto>
{
    /// <inheritdoc/>
    public async Task<IdCreadoDto> HandleAsync(RegistrarResultadoRevalorizacionCommand comando, CancellationToken ct)
    {
        // (a) Cargar la revalorización
        var revalorizacion = await revalorizaciones.GetByIdAsync(comando.RevalorizacionId, ct)
            ?? throw new NoEncontradoException("revalorización", comando.RevalorizacionId);

        // (b) Solo el nutricionista ejecutante registra el resultado (RK-11)
        if (actor.Id != revalorizacion.NutricionistaId)
        {
            throw new ReglaDeNegocioException(
                "Solo el nutricionista que ejecuta la revalorización registra su resultado.",
                "RK-11");
        }

        // (c) Cargar la valoración revisada
        var valoracionRevisada = await valoraciones.GetByIdAsync(revalorizacion.ValoracionRevisadaId, ct)
            ?? throw new NoEncontradoException("valoración", revalorizacion.ValoracionRevisadaId);

        // (d) Crear la nueva valoración nutricional (RS-10: misma asignación, emitida por el ejecutor)
        var nuevaValoracion = new ValoracionNutricional(
            Guid.NewGuid(),
            valoracionRevisada.MenuId,
            valoracionRevisada.PacienteId,
            actor.Id,
            comando.ResultadoNutricionalId,
            DateTime.UtcNow);

        // (e) Enlazar la valoración generada en la revalorización
        revalorizacion.RegistrarValoracionGenerada(nuevaValoracion.Id);

        // (f) Persistir: AddAsync de la valoración y CommitAsync único
        // La revalorización cargada ya está trackeada por EF Core; si no lo estuviera, Repository.Update la adjuntaría.
        await valoraciones.AddAsync(nuevaValoracion, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        // (g) Invalidar caché de valoraciones recientes del paciente (informe §2)
        await cache.InvalidarAsync(CacheDeValoracionesRecientes.Clave(valoracionRevisada.PacienteId), ct);

        // (h) Devolver el id de la valoración generada
        return new IdCreadoDto(nuevaValoracion.Id);
    }
}