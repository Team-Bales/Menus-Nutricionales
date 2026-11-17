using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;

namespace Menus.Application.Nutricionistas.AutorizarDieta;

/// <summary>Autoriza al nutricionista para la dieta, salvo que ya lo esté (operación idempotente, #71).</summary>
internal sealed class AutorizarDietaHandler(
    INutricionistaRepository nutricionistas,
    IDietaRepository dietas,
    IUnitOfWork unidadDeTrabajo) : IHandler<AutorizarDietaCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el nutricionista o la dieta no existen.</exception>
    public async Task<Unidad> HandleAsync(AutorizarDietaCommand solicitud, CancellationToken ct)
    {
        var nutricionista = await nutricionistas.GetByIdAsync(solicitud.NutricionistaId, ct)
            ?? throw new NoEncontradoException("nutricionista", solicitud.NutricionistaId);

        // Idempotente: autorizar una dieta ya autorizada no es un error (#71), a diferencia de lo que exige
        // Nutricionista.AutorizarDieta (RC-06) para cualquier otro llamador.
        if (nutricionista.DietasAutorizadas.Any(a => a.DietaId == solicitud.DietaId))
        {
            return Unidad.Valor;
        }

        _ = await dietas.GetByIdAsync(solicitud.DietaId, ct)
            ?? throw new NoEncontradoException("dieta", solicitud.DietaId);

        nutricionista.AutorizarDieta(solicitud.DietaId);
        await unidadDeTrabajo.CommitAsync(ct);

        return Unidad.Valor;
    }
}
