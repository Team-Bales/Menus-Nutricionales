using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Nutricionistas.RevocarDieta;

/// <summary>Revoca la autorización del nutricionista para la dieta.</summary>
internal sealed class RevocarDietaHandler(INutricionistaRepository nutricionistas, IUnitOfWork unidadDeTrabajo)
    : IHandler<RevocarDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si no estaba autorizado para esa dieta.</exception>
    public async Task<Unidad> HandleAsync(RevocarDietaCommand solicitud, CancellationToken ct)
    {
        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.NutricionistaId);

        nutricionista.RevocarDieta(solicitud.DietaId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
