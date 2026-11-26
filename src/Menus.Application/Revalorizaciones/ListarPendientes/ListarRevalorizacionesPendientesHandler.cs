using Menus.Application.Comun;
using Menus.Application.Revalorizaciones.ListarPendientes;
using Menus.Domain;
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
/// <remarks>
/// La ve el personal (nutricionista o administrador) y el paciente que la solicitó, que es el de la valoración revisada
/// (RS-09, #267). El identificador de la ruta es el de la revalorización, así que el paciente se busca a través de la
/// valoración: no alcanza con una política de la API.
/// </remarks>
internal sealed class ObtenerRevalorizacionHandler(
    IRevalorizacionRepository revalorizaciones,
    IValoracionRepository valoraciones,
    IIdentidadAutenticada identidad) : IHandler<ObtenerRevalorizacionQuery, RevalorizacionDto>
{
    /// <exception cref="NoEncontradoException">Si la revalorización no existe.</exception>
    /// <exception cref="AccesoDenegadoException">Si la pide un paciente que no es el de la valoración revisada.</exception>
    public async Task<RevalorizacionDto> HandleAsync(ObtenerRevalorizacionQuery consulta, CancellationToken ct)
    {
        var revalorizacion = await revalorizaciones.GetByIdAsync(consulta.Id, ct)
            ?? throw new NoEncontradoException("revalorización", consulta.Id);

        // Al personal no hace falta buscarle la valoración: solo al paciente, para saber si es el suyo.
        if (!identidad.EsPersonal())
        {
            // La valoración existe (FK en RESTRICT); si faltara, se niega en lugar de mostrarla.
            var revisada = await valoraciones.GetByIdAsync(revalorizacion.ValoracionRevisadaId, ct);
            if (revisada is null || !identidad.EsElPaciente(revisada.PacienteId))
            {
                throw new AccesoDenegadoException("Solo el paciente de la valoración revisada y el personal ven esta revalorización.");
            }
        }

        return new RevalorizacionDto(
            revalorizacion.Id,
            revalorizacion.FechaSolicitud,
            revalorizacion.NutricionistaId,
            revalorizacion.ValoracionRevisadaId,
            revalorizacion.ValoracionGeneradaId);
    }
}
