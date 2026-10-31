namespace Menus.Infrastructure.Security;

/// <summary>Nombres de los claims que viajan en el token JWT.</summary>
public static class JwtClaimTypes
{
    /// <summary>Identificador del usuario autenticado.</summary>
    public const string Subject = "sub";

    /// <summary>Rol del usuario (<see cref="JwtRoles"/>).</summary>
    public const string Role = "role";

    /// <summary>Especialidad del nutricionista.</summary>
    public const string Especialidad = "especialidad";

    /// <summary>Marca adicional del nutricionista que ocupa el cargo de jefe de nutrición.</summary>
    public const string JefeNutricion = "jefe_nutricion";
}

/// <summary>Roles que puede llevar el claim <see cref="JwtClaimTypes.Role"/>.</summary>
public static class JwtRoles
{
    /// <summary>Administrador del sistema.</summary>
    public const string Admin = "Admin";

    /// <summary>Nutricionista.</summary>
    public const string Nutricionista = "Nutricionista";

    /// <summary>Paciente.</summary>
    public const string Paciente = "Paciente";

    /// <summary>
    /// Rol de nutricionista que puede validar menús. No se emite como claim <c>role</c>:
    /// se representa con el claim adicional <see cref="JwtClaimTypes.JefeNutricion"/>.
    /// </summary>
    public const string JefeNutricion = "JefeNutricion";

    /// <summary>Indica si <paramref name="role"/> es uno de los roles válidos del claim <c>role</c>.</summary>
    /// <param name="role">Rol a comprobar.</param>
    /// <returns><see langword="true"/> si el rol es <see cref="Admin"/>, <see cref="Nutricionista"/> o <see cref="Paciente"/>.</returns>
    public static bool IsValid(string? role) =>
        role is Admin or Nutricionista or Paciente;
}
