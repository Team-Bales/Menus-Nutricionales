using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Excepciones;

namespace Menus.Application.Catalogo.Platos;

/// <summary>
/// Comprobaciones previas que comparten los casos de uso que tocan la composición de un plato (ADR-010, §3): dan un
/// mensaje claro antes de que la base rechace el cambio.
/// </summary>
internal static class ComposicionDelPlato
{
    /// <summary>
    /// Exige que todos los identificadores sean alimentos existentes: RR-02 si alguno no existe, RS-12 si alguno es
    /// un plato (la composición es de un solo nivel).
    /// </summary>
    /// <param name="ids">Alimentos que se quieren usar como ingredientes.</param>
    /// <param name="alimentos">Repositorio de alimentos.</param>
    /// <param name="items">Repositorio de ítems, para distinguir un plato de un identificador inexistente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Los alimentos, para quien necesite sus datos (por ejemplo, el nombre para la ficha).</returns>
    /// <exception cref="ReglaDeNegocioException">Si alguno no existe (RR-02) o es un plato (RS-12).</exception>
    public static async Task<IReadOnlyList<Alimento>> ExigirAlimentosAsync(
        IReadOnlyCollection<Guid> ids,
        IAlimentoRepository alimentos,
        IItemAlimentarioRepository items,
        CancellationToken ct)
    {
        var existentes = await alimentos.GetByIdsAsync(ids, ct);
        var encontrados = existentes.Select(a => a.Id).ToHashSet();

        foreach (var id in ids.Where(id => !encontrados.Contains(id)))
        {
            if (await items.GetByIdAsync(id, ct) is Plato)
            {
                throw new ReglaDeNegocioException(
                    $"{id} es un plato: los ingredientes de un plato son alimentos simples.",
                    "RS-12");
            }

            throw new ReglaDeNegocioException($"No existe el alimento {id} que se quiere usar como ingrediente.", "RR-02");
        }

        return existentes;
    }

    /// <summary>
    /// Exige que quien cambia la composición sea el nutricionista que describió el plato (RS-02).
    /// </summary>
    /// <remarks>
    /// El dominio lo exige para reclasificar un ítem; la composición también es parte de la descripción del plato,
    /// así que se aplica la misma regla (ADR-012). Lo comprueba el caso de uso porque <see cref="Plato.AgregarIngrediente"/> y
    /// <see cref="Plato.QuitarIngrediente"/> no reciben al actor.
    /// </remarks>
    /// <param name="plato">Plato que se modifica.</param>
    /// <param name="nutricionistaId">Actor de la petición.</param>
    /// <exception cref="ReglaDeNegocioException">Si el actor no es quien describió el plato.</exception>
    public static void ExigirDescriptor(Plato plato, Guid nutricionistaId)
    {
        if (plato.NutricionistaId != nutricionistaId)
        {
            throw new ReglaDeNegocioException(
                "Solo el nutricionista que describió el plato puede cambiar su composición (RS-02).",
                "RS-02");
        }
    }
}
