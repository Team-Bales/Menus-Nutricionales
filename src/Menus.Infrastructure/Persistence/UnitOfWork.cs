using Menus.Domain.Abstractions;

namespace Menus.Infrastructure.Persistence;

/// <summary>
/// Implementación EF Core de <see cref="IUnitOfWork"/> (ADR-004): confirma en una sola transacción todo lo que los
/// repositorios del caso de uso dejaron en el <see cref="MenusDbContext"/>.
/// </summary>
/// <remarks>
/// Los repositorios y esta unidad comparten el mismo <see cref="MenusDbContext"/> porque los tres se registran con
/// ámbito de petición. Los triggers diferidos (RK-03, RK-07, RK-09, RS-07, RS-14 y RS-16) saltan aquí, al confirmar
/// (ADR-010, §5).
/// </remarks>
/// <param name="dbContext">Contexto de la petición.</param>
public sealed class UnitOfWork(MenusDbContext dbContext) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
