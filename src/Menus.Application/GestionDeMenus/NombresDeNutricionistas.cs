using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.GestionDeMenus;

/// <summary>Busca el nombre de los nutricionistas que crearon o dictaminaron los menús de una dieta.</summary>
internal static class NombresDeNutricionistas
{
    /// <summary>
    /// Devuelve el nombre de cada uno de <paramref name="ids"/>. Primero busca a los autorizados para la dieta, en una
    /// sola consulta: quien crea un menú y quien lo dictamina tienen que estarlo (RS-03). Solo si a alguno le retiraron
    /// la autorización después, lo busca por su identificador.
    /// </summary>
    /// <param name="nutricionistas">Repositorio de nutricionistas.</param>
    /// <param name="dietaId">Dieta de los menús.</param>
    /// <param name="ids">Creadores y revisores de los menús.</param>
    /// <param name="ct">Token de cancelación.</param>
    public static async Task<IReadOnlyDictionary<Guid, string>> BuscarAsync(
        INutricionistaRepository nutricionistas,
        Guid dietaId,
        IEnumerable<Guid> ids,
        CancellationToken ct)
    {
        var buscados = ids.ToHashSet();
        var nombres = (await nutricionistas.GetByDietaAsync(dietaId, ct))
            .Where(n => buscados.Contains(n.Id))
            .ToDictionary(n => n.Id, n => n.Nombre);

        foreach (var id in buscados.Where(id => !nombres.ContainsKey(id)).ToList())
        {
            // Existe: el menú y el dictamen lo referencian con claves foráneas en RESTRICT.
            var nutricionista = await nutricionistas.GetByIdAsync(id, ct);
            nombres[id] = nutricionista!.Nombre;
        }

        return nombres;
    }

    /// <summary>Los nutricionistas que nombra un menú: su creador y, si tiene dictamen, su revisor.</summary>
    /// <param name="menu">Menú del dominio, con su dictamen.</param>
    public static IEnumerable<Guid> De(Menu menu)
    {
        yield return menu.NutricionistaCreadorId;
        if (menu.Validacion is { } validacion)
        {
            yield return validacion.NutricionistaId;
        }
    }
}
