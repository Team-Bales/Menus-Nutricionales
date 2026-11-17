using Menus.Domain.Entidades;

namespace Menus.Domain.Abstractions.Repositories;

/// <summary>
/// Repositorio de <see cref="Sala"/>. Además de las operaciones base, expone la comprobación de nombre único que
/// necesita la gestión de salas (issue #72).
/// </summary>
/// <remarks>No hay <c>UNIQUE</c> en la base para <c>salas.nombre</c> (RD-08 lo exige solo en los nomencladores,
/// informe §6.1): el nombre único se valida en el caso de uso, con esta consulta.</remarks>
public interface ISalaRepository : IRepository<Sala, Guid>
{
    /// <summary>Si ya existe una sala con ese nombre, sin distinguir mayúsculas ni espacios en los bordes.</summary>
    /// <param name="nombre">Nombre a comprobar.</param>
    /// <param name="excluirId">Si se indica, no cuenta la sala con ese id (al actualizar, para no chocar con el propio nombre).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default);
}
