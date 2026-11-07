namespace Menus.Domain;

/// <summary>
/// Roles de usuario del sistema. Fuente única de verdad para los roles en el dominio.
/// </summary>
public static class RolesUsuario
{
    /// <summary>Administrador del sistema.</summary>
    public const string Admin = "Admin";

    /// <summary>Nutricionista.</summary>
    public const string Nutricionista = "Nutricionista";

    /// <summary>Paciente.</summary>
    public const string Paciente = "Paciente";

    /// <summary>
    /// Indica si <paramref name="rol"/> es uno de los roles válidos del sistema.
    /// </summary>
    /// <param name="rol">Rol a comprobar.</param>
    /// <returns><see langword="true"/> si el rol es <see cref="Admin"/>, <see cref="Nutricionista"/> o <see cref="Paciente"/>.</returns>
    public static bool EsValido(string? rol) =>
        rol is Admin or Nutricionista or Paciente;
}
