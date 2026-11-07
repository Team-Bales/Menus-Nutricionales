using Menus.Domain.Abstractions;
using Menus.Domain.Excepciones;

namespace Menus.Domain.Entidades;

/// <summary>
/// Usuario del sistema para autenticación y autorización.
/// </summary>
/// <remarks>
/// La igualdad se determina por <see cref="Entity{TId}.Id"/>.
/// El email se almacena normalizado (lowercase + trim) para garantizar unicidad case-insensitive.
/// <para>
/// Reglas de <see cref="EntidadId"/> según el rol:
/// <list type="bullet">
/// <item><description><see cref="RolesUsuario.Admin"/>: <see cref="EntidadId"/> debe ser <see langword="null"/>.</description></item>
/// <item><description><see cref="RolesUsuario.Nutricionista"/> o <see cref="RolesUsuario.Paciente"/>: <see cref="EntidadId"/> debe ser no nulo y referenciar al nutricionista o paciente correspondiente.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Usuario : Entity<Guid>
{
    private Usuario(Guid id, string email, string passwordHash, string rol, Guid? entidadId) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Rol = rol;
        EntidadId = entidadId;
    }

    /// <summary>Email normalizado (lowercase + trim), único en el sistema.</summary>
    public string Email { get; }

    /// <summary>Hash BCrypt de la contraseña.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>Rol del usuario: <see cref="RolesUsuario.Admin"/>, <see cref="RolesUsuario.Nutricionista"/> o <see cref="RolesUsuario.Paciente"/>.</summary>
    public string Rol { get; }

    /// <summary>
    /// Identificador de la entidad de dominio asociada (nutricionista o paciente).
    /// <see langword="null"/> para administradores.
    /// </summary>
    public Guid? EntidadId { get; }

    /// <summary>
    /// Crea un nuevo usuario con email normalizado.
    /// </summary>
    /// <param name="id">Identificador único.</param>
    /// <param name="email">Email del usuario (se normaliza a lowercase + trim).</param>
    /// <param name="passwordHash">Hash BCrypt de la contraseña.</param>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="entidadId">Identificador de la entidad de dominio asociada, o <see langword="null"/> para administradores.</param>
    /// <returns>Nueva instancia de <see cref="Usuario"/>.</returns>
    /// <exception cref="DatoInvalidoException">
    /// Si <paramref name="email"/> es nulo o solo espacios,
    /// si <paramref name="passwordHash"/> es nulo o vacío,
    /// si <paramref name="rol"/> no es un rol válido,
    /// o si la combinación <paramref name="rol"/> + <paramref name="entidadId"/> viola las reglas de dominio.
    /// </exception>
    public static Usuario Crear(Guid id, string email, string passwordHash, string rol, Guid? entidadId)
    {
        DatoInvalidoException.SiVacio(email);
        DatoInvalidoException.SiVacio(passwordHash);

        var emailNormalizado = NormalizarEmail(email);

        if (!RolesUsuario.EsValido(rol))
        {
            throw new DatoInvalidoException(nameof(rol), $"El rol '{rol}' no es válido.");
        }

        ValidarReglaEntidadId(rol, entidadId);

        return new Usuario(id, emailNormalizado, passwordHash, rol, entidadId);
    }

    /// <summary>
    /// Actualiza el hash de la contraseña (para cambio de contraseña futuro).
    /// </summary>
    /// <param name="nuevoPasswordHash">Nuevo hash BCrypt.</param>
    /// <exception cref="DatoInvalidoException">Si <paramref name="nuevoPasswordHash"/> es nulo o vacío.</exception>
    public void ActualizarPasswordHash(string nuevoPasswordHash)
    {
        DatoInvalidoException.SiVacio(nuevoPasswordHash);
        PasswordHash = nuevoPasswordHash;
    }

    /// <summary>Normaliza un email a lowercase + trim para almacenamiento y búsqueda consistentes.</summary>
    /// <param name="email">Email a normalizar.</param>
    /// <returns>Email normalizado.</returns>
    public static string NormalizarEmail(string email) =>
        email.Trim().ToLowerInvariant();

    /// <summary>
    /// Valida que la combinación rol + EntidadId cumpla las reglas de dominio.
    /// </summary>
    private static void ValidarReglaEntidadId(string rol, Guid? entidadId)
    {
        if (rol == RolesUsuario.Admin)
        {
            if (entidadId.HasValue)
            {
                throw new DatoInvalidoException(nameof(entidadId), "Un administrador no tiene entidad asociada (EntidadId debe ser null).");
            }
        }
        else // Nutricionista o Paciente
        {
            if (!entidadId.HasValue)
            {
                throw new DatoInvalidoException(nameof(entidadId), $"Un {rol} debe tener una entidad asociada (EntidadId no puede ser null).");
            }
        }
    }
}
