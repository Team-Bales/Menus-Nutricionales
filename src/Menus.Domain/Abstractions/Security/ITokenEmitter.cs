namespace Menus.Domain.Abstractions.Security;

/// <summary>
/// Resultado de la emisión de un token de acceso.
/// </summary>
/// <param name="Token">Token JWT serializado.</param>
/// <param name="ExpiraUtc">Instante UTC en que el token expira.</param>
public sealed record TokenEmitido(string Token, DateTime ExpiraUtc);

/// <summary>
/// Contrato para la emisión de tokens de acceso (JWT).
/// </summary>
public interface ITokenEmitter
{
    /// <summary>
    /// Emite un token de acceso para el usuario autenticado.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario (claim <c>sub</c>).</param>
    /// <param name="rol">Rol del usuario: <c>Admin</c>, <c>Nutricionista</c> o <c>Paciente</c>.</param>
    /// <param name="entidadId">Identificador de la entidad de dominio asociada (nutricionista o paciente), si corresponde.</param>
    /// <param name="especialidad">Especialidad del nutricionista, si el rol es <c>Nutricionista</c>.</param>
    /// <param name="esJefeNutricion">Indica si el nutricionista ocupa el cargo de jefe de nutrición.</param>
    /// <returns>Token emitido con su expiración UTC.</returns>
    /// <exception cref="ArgumentException">Si <paramref name="rol"/> no es válido, o si <paramref name="esJefeNutricion"/> es <see langword="true"/> para un rol distinto de <c>Nutricionista</c>.</exception>
    TokenEmitido Emitir(
        Guid usuarioId,
        string rol,
        Guid? entidadId = null,
        string? especialidad = null,
        bool esJefeNutricion = false);
}
