using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.SolicitarRevalorizacion;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Revalorizaciones.SolicitarRevalorizacion;

/// <summary>Manejador de <see cref="SolicitarRevalorizacionCommand"/>.</summary>
internal sealed class SolicitarRevalorizacionHandler(
    IRevalorizacionRepository revalorizaciones,
    IValoracionRepository valoraciones,
    INutricionistaRepository nutricionistas,
    IUnitOfWork unidadDeTrabajo) : IHandler<SolicitarRevalorizacionCommand, IdCreadoDto>
{
    /// <inheritdoc/>
    public async Task<IdCreadoDto> HandleAsync(SolicitarRevalorizacionCommand comando, CancellationToken ct)
    {
        // Verificar que la valoración revisada existe
        var valoracionRevisada = await valoraciones.GetByIdAsync(comando.ValoracionRevisadaId, ct)
            ?? throw new NoEncontradoException("valoración", comando.ValoracionRevisadaId);

        // Verificar que el nutricionista existe
        var nutricionista = await nutricionistas.GetByIdAsync(comando.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", comando.NutricionistaId);

        // Crear la revalorización con ValoracionGeneradaId en null (resultado nulo)
        var revalorizacion = new Revalorizacion(
            Guid.NewGuid(),
            DateTime.UtcNow,
            comando.NutricionistaId,
            comando.ValoracionRevisadaId);

        await revalorizaciones.AddAsync(revalorizacion, ct);
        await unidadDeTrabajo.CommitAsync(ct);

        return new IdCreadoDto(revalorizacion.Id);
    }
}