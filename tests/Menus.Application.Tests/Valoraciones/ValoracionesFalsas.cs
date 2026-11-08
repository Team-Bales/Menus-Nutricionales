using Menus.Application.Tests.Catalogo.Dietas;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.Valoraciones;

/// <summary>
/// Valoraciones en memoria para probar los manejadores del #86 sin EF Core (ADR-010, §9). Las dietas, los
/// pacientes y los menús los pone <see cref="BancoDeDietasFalso"/>, de cuyos menús resuelve la dieta, igual que
/// el repositorio real resuelve <c>GetByDietaAsync</c> con un <c>join</c> (ADR-010, §9).
/// </summary>
/// <param name="banco">Banco de dietas, pacientes y menús, para resolver la dieta de un menú.</param>
internal sealed class ValoracionesFalsas(BancoDeDietasFalso banco) : IValoracionRepository
{
    public List<ValoracionNutricional> Valoraciones { get; } = [];

    public Task<ValoracionNutricional?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Valoraciones.Find(v => v.Id == id));

    public Task<IReadOnlyList<ValoracionNutricional>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>([.. Valoraciones]);

    public Task AddAsync(ValoracionNutricional entity, CancellationToken cancellationToken = default)
    {
        Valoraciones.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(ValoracionNutricional entity) => Valoraciones.Remove(entity);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>(
            [.. Valoraciones.Where(v => v.PacienteId == pacienteId).OrderByDescending(v => v.FechaValoracion)]);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>(
            [.. Valoraciones.Where(v => DietaDelMenu(v.MenuId) == dietaId).OrderByDescending(v => v.FechaValoracion)]);

    public Task<IReadOnlyList<ValoracionNutricional>> GetByPacienteYDietaAsync(
        Guid pacienteId, Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ValoracionNutricional>>(
            [.. Valoraciones
                .Where(v => v.PacienteId == pacienteId && DietaDelMenu(v.MenuId) == dietaId)
                .OrderByDescending(v => v.FechaValoracion)]);

    private Guid DietaDelMenu(Guid menuId) => banco.Menus.Single(m => m.Id == menuId).DietaId;
}
