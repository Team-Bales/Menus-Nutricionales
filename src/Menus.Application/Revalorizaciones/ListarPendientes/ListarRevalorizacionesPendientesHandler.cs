using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Revalorizaciones.ListarPendientes;

/// <summary>Manejador de <see cref="ListarRevalorizacionesPendientesQuery"/>.</summary>
internal sealed class ListarRevalorizacionesPendientesHandler(
    IRevalorizacionRepository revalorizaciones,
    INutricionistaRepository nutricionistas) : IHandler<ListarRevalorizacionesPendientesQuery, IReadOnlyList<RevalorizacionDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RevalorizacionDto>> HandleAsync(ListarRevalorizacionesPendientesQuery consulta, CancellationToken ct)
    {
        // Verificar que el nutricionista existe
        var nutricionista = await nutricionistas.GetByIdAsync(consulta.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", consulta.NutricionistaId);

        // Obtener las pendientes
        var pendientes = await revalorizaciones.GetPendientesByNutricionistaAsync(consulta.NutricionistaId, ct);

        // Mapear a DTO
        return [.. pendientes.Select(r => new RevalorizacionDto(
            r.Id,
            r.FechaSolicitud,
            r.NutricionistaId,
            r.ValoracionRevisadaId,
            r.ValoracionGeneradaId))];
    }
}

/// <summary>Manejador de <see cref="ObtenerRevalorizacionQuery"/>.</summary>
internal sealed class ObtenerRevalorizacionHandler(
    IRevalorizacionRepository revalorizaciones) : IHandler<ObtenerRevalorizacionQuery, RevalorizacionDto>
{
    /// <inheritdoc/>
    public async Task<RevalorizacionDto> HandleAsync(ObtenerRevalorizacionQuery consulta, CancellationToken ct)
    {
        var revalorizacion = await revalorizaciones.GetByIdAsync(consulta.Id, ct)
            ?? throw new NoEncontradoException("revalorización", consulta.Id);

        return new RevalorizacionDto(
            revalorizacion.Id,
            revalorizacion.FechaSolicitud,
            revalorizacion.NutricionistaId,
            revalorizacion.ValoracionRevisadaId,
            revalorizacion.ValoracionGeneradaId);
    }
}