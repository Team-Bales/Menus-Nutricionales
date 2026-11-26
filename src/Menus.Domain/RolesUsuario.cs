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
    /// Roles del personal, <see cref="Nutricionista"/> y <see cref="Admin"/>, que ve los datos de todos los pacientes.
    /// </summary>
    /// <remarks>
    /// Es la única definición de la regla «el personal ve todo; un paciente, solo lo suyo» (#316). La usan la política
    /// <c>NutricionistaOAdmin</c> y la de <c>PacientePropioOPersonal</c> en la API, y los casos de uso que comprueban el
    /// dueño del dato a través de <see cref="EsPersonal"/>. Si el personal cambia, se cambia solo aquí.
    /// </remarks>
    public static readonly IReadOnlyList<string> Personal = [Nutricionista, Admin];

    /// <summary>
    /// Indica si <paramref name="rol"/> es uno de los roles válidos del sistema.
    /// </summary>
    /// <param name="rol">Rol a comprobar.</param>
    /// <returns><see langword="true"/> si el rol es <see cref="Admin"/>, <see cref="Nutricionista"/> o <see cref="Paciente"/>.</returns>
    public static bool EsValido(string? rol) =>
        rol is Admin or Nutricionista or Paciente;

    /// <summary>
    /// Indica si <paramref name="rol"/> es del personal (<see cref="Personal"/>). Compara exacto, igual que
    /// <see cref="EsValido"/>: el rol viaja en el token con estos mismos valores.
    /// </summary>
    /// <param name="rol">Rol a comprobar.</param>
    /// <returns><see langword="true"/> si el rol es <see cref="Nutricionista"/> o <see cref="Admin"/>.</returns>
    public static bool EsPersonal(string? rol) => rol is not null && Personal.Contains(rol);
}
