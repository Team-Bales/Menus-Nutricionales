using Menus.Client.Catalogo;
using Menus.Client.MenusGenerados;

namespace Menus.Client.Services;

/// <summary>Contrato de la UI con los endpoints de menús.</summary>
/// <remarks>
/// Cada método llama a un endpoint de la API con el token de la sesión. Si la API responde con un error, lanza
/// <see cref="ApiException"/> con el mensaje que se muestra en la página.
/// </remarks>
public interface IMenusApi
{
    /// <summary>
    /// Una página de menús de una dieta, opcionalmente en un estado
    /// (<c>GET api/menus?dietaId=…&amp;estado=…</c>).
    /// </summary>
    Task<Pagina<Menu>> ListarMenusAsync(Guid dietaId, string? estado, int page, int size, CancellationToken ct = default);

    /// <summary>Un menú con su parametrización, sus grupos y sus ítems (<c>GET api/menus/{id}</c>).</summary>
    Task<MenuDetalle> ObtenerMenuAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Genera automáticamente un menú y devuelve su id
    /// (<c>POST api/dietas/{dietaId}/menus/generacion-automatica</c>).
    /// </summary>
    Task<Guid> GenerarMenuAsync(GeneracionDeMenu generacion, CancellationToken ct = default);
}
