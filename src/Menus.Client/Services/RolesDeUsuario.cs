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
    public const string Personal = Admin + "," + Nutricionista;
}
