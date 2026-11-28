using Menus.Client.GestionMenus;

namespace Menus.Client.Services;

/// <summary>Contrato HTTP de las consultas y el dictamen de menús (#80, #82).</summary>
public interface IMenusApi
{
    /// <summary>Obtiene el detalle de un menú.</summary>
    Task<MenuDetalle> ObtenerAsync(Guid menuId, CancellationToken ct = default);

    /// <summary>Registra el dictamen de aprobación o rechazo del jefe de nutrición.</summary>
    Task ValidarAsync(Guid menuId, bool aprobado, DictamenFormulario formulario, CancellationToken ct = default);
}
