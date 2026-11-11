using Menus.Application.Catalogo.Dietas.DisponibilidadCalorica;
using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Alimentos.EliminarAlimento;

/// <summary>Elimina el alimento si no está en uso, y después su ficha nutricional (RM-09).</summary>
/// <remarks>
/// El uso se comprueba antes para dar un mensaje claro; las claves foráneas en <c>RESTRICT</c> siguen siendo la
/// última barrera (ADR-010, §3). La ficha se borra después de confirmar (ADR-010, §5). También invalida la
/// disponibilidad calórica de todas las dietas (informe, «Consultas recurrentes»; #101): la baja de un ítem del
/// banco cambia con qué cuentan.
/// </remarks>
internal sealed class EliminarAlimentoHandler(
    IItemAlimentarioRepository items,
    IAlimentoRepository alimentos,
    IDietaRepository dietas,
    IUnitOfWork unidadDeTrabajo,
    IFichasNutricionales fichas,
    ICache cache) : IHandler<EliminarAlimentoCommand, Unidad>
{
    /// <exception cref="NoEncontradoException">Si el alimento no existe.</exception>
    /// <exception cref="ReglaDeNegocioException">Si es ingrediente de un plato (RR-04) o figura en un menú (RR-22).</exception>
    public async Task<Unidad> HandleAsync(EliminarAlimentoCommand solicitud, CancellationToken ct)
    {
        var alimento = await alimentos.GetByIdAsync(solicitud.Id, ct)
            ?? throw new NoEncontradoException("alimento", solicitud.Id);

        if (await alimentos.FormaParteDeAlgunPlatoAsync(alimento.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"El alimento «{alimento.Nombre}» es ingrediente de algún plato: hay que quitarlo de su composición antes de eliminarlo.",
                "RR-04");
        }

        if (await items.FiguraEnAlgunMenuAsync(alimento.Id, ct))
        {
            throw new ReglaDeNegocioException(
                $"El alimento «{alimento.Nombre}» figura en algún menú, y el historial del menú depende de él: no se puede eliminar.",
                "RR-22");
        }

        alimentos.Remove(alimento);
        await unidadDeTrabajo.CommitAsync(ct);
        await fichas.EliminarAsync(alimento);

        await CacheDeDisponibilidadCalorica.InvalidarTodasAsync(dietas, cache, ct);

        return Unidad.Valor;
    }
}
