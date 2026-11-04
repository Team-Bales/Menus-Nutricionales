using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación EF Core de <see cref="IMenuRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GetByIdAsync"/> carga el agregado completo y rastreado (ítems, distribución, grupos cubiertos,
/// dictamen y asignaciones con sus servicios y consumos), porque es lo que se usa para modificarlo: las reglas del
/// agregado (no asignar dos veces, servir solo a un paciente asignado…) deciden sobre esas colecciones, y sin
/// rastreo los cambios no se guardarían. Los listados son de solo lectura (<c>AsNoTracking</c>) y traen la
/// parametrización, que es lo que muestran.
/// </para>
/// <para>
/// El estado del menú se deriva de su dictamen (<see cref="Menu.EsFinal"/>, <see cref="Menu.EsPendiente"/>), pero
/// esas propiedades se calculan en C# y EF no puede traducirlas a SQL. Por eso los filtros usan la navegación
/// <see cref="Menu.Validacion"/>, que es lo mismo expresado sobre la tabla.
/// </para>
/// </remarks>
public sealed class MenuRepository(MenusDbContext dbContext) : IMenuRepository
{
    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Menus
            .Include(m => m.Items)
            .Include(m => m.Distribuciones)
            .Include(m => m.GruposNutricionales)
            .Include(m => m.Asignaciones).ThenInclude(a => a.Servicios).ThenInclude(s => s.Consumos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Menu>> ListAsync(CancellationToken cancellationToken = default)
        => await Listado().ToListAsync(cancellationToken);

    public async Task AddAsync(Menu entity, CancellationToken cancellationToken = default)
        => await dbContext.Menus.AddAsync(entity, cancellationToken);

    public void Remove(Menu entity)
        => dbContext.Menus.Remove(entity);

    public async Task<IReadOnlyList<Menu>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await Listado()
            .Where(m => m.DietaId == dietaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetByNutricionistaCreadorAsync(Guid nutricionistaId, CancellationToken cancellationToken = default)
        => await Listado()
            .Where(m => m.NutricionistaCreadorId == nutricionistaId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => await Listado()
            .Where(m => m.Asignaciones.Any(a => a.PacienteId == pacienteId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetFinalesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await Listado()
            .Where(m => m.DietaId == dietaId && m.Validacion != null && m.Validacion.Aprobado)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => await Listado()
            .Where(m => m.DietaId == dietaId && m.Validacion == null)
            .ToListAsync(cancellationToken);

    /// <summary>Menús de solo lectura con su parametrización (el dictamen llega por AutoInclude).</summary>
    private IQueryable<Menu> Listado()
        => dbContext.Menus
            .AsNoTracking()
            .Include(m => m.Distribuciones)
            .Include(m => m.GruposNutricionales)
            .AsSplitQuery();
}
