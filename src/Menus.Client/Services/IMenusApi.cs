using Menus.Client.Catalogo;
using Menus.Client.GestionDeMenus;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints de menús (#80).</summary>
/// <remarks>
/// Cada método llama a un endpoint de la API con el token de la sesión. Si la API responde con un error, lanza
/// <see cref="ApiException"/> con el <c>detail</c> del <c>ProblemDetails</c>, que se muestra en la página.
/// </remarks>
public interface IMenusApi
{
    /// <summary>
    /// Genera un menú automáticamente para la dieta indicada
    /// (<c>POST api/dietas/{dietaId}/menus/generacion-automatica</c>) y devuelve su identificador.
    /// </summary>
    /// <param name="solicitud">Dieta, cantidad total, distribución y grupos a cubrir.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<Guid> GenerarAutomaticamenteAsync(SolicitudDeGeneracion solicitud, CancellationToken ct = default);

    /// <summary>Una página de menús de una dieta (<c>GET api/menus?dietaId=…</c>), del más reciente al más antiguo.</summary>
    /// <param name="dietaId">Dieta de los menús.</param>
    /// <param name="page">Página, desde 1.</param>
    /// <param name="size">Menús por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<Pagina<MenuResumen>> ListarPorDietaAsync(Guid dietaId, int page, int size, CancellationToken ct = default);

    /// <summary>Un menú con su parametrización y sus ítems (<c>GET api/menus/{id}</c>).</summary>
    /// <param name="id">Identificador del menú.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<MenuDetalle> ObtenerAsync(Guid id, CancellationToken ct = default);
}
