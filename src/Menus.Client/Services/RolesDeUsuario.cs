using System.Security.Claims;

namespace Menus.Client.Services;

/// <summary>
/// Roles que viajan en el claim <c>role</c> del JWT, con los mismos valores que
/// <c>Menus.Domain.RolesUsuario</c>, y las combinaciones que usan las páginas.
/// </summary>
/// <remarks>
/// Deben coincidir con las políticas de <c>Menus.Api.Auth.Politicas</c>: así la UI no muestra
/// pantallas ni botones que la API respondería con 403.
/// <list type="bullet">
///   <item>Crear, editar o borrar alimentos y platos → <c>SoloNutricionista</c>.</item>
///   <item>Crear o editar dietas, sus grupos y restricciones, y los nomencladores → <c>SoloAdmin</c>.</item>
///   <item>Consultar el catálogo → cualquier usuario autenticado; la UI lo limita al personal.</item>
/// </list>
/// </remarks>
public static class RolesDeUsuario
{
    /// <summary>Administrador del sistema.</summary>
    public const string Admin = "Admin";

    /// <summary>Nutricionista.</summary>
    public const string Nutricionista = "Nutricionista";

    /// <summary>Paciente.</summary>
    public const string Paciente = "Paciente";

    /// <summary>Personal que consulta el catálogo (formato de <c>Roles</c> de <c>[Authorize]</c>).</summary>
    /// <remarks>
    /// Es el mismo personal que <c>Menus.Domain.RolesUsuario.Personal</c> (#316). El cliente no referencia el dominio,
    /// así que aquí está su copia, y <see cref="EsPersonal"/> la lee para no repetir los dos roles.
    /// </remarks>
    public const string Personal = Admin + "," + Nutricionista;

    /// <summary>Indica si el usuario de la sesión es del personal (<see cref="Personal"/>).</summary>
    /// <param name="usuario">Usuario de la sesión, con un claim de rol por valor.</param>
    /// <returns><see langword="true"/> si tiene alguno de los roles de <see cref="Personal"/>.</returns>
    public static bool EsPersonal(ClaimsPrincipal usuario) => Personal.Split(',').Any(usuario.IsInRole);
}
