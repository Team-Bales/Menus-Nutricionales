using Menus.Application.Comun;
using Menus.Application.Reportes;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Reportes;

/// <summary>
/// Banco de datos en memoria para probar los manejadores de reportes sin EF Core.
/// </summary>
internal sealed class BancoDeReportesFalso
{
    public List<Dieta> Dietas { get; } = [];
    public List<Nutricionista> Nutricionistas { get; } = [];
    public List<Menu> Menus { get; } = [];

    public DietasFalsas DietasRepo => new(this);
    public NutricionistasFalsos NutricionistasRepo => new(this);
    public MenusFalsos MenusRepo => new(this);

    public Dieta AgregarDieta(string nombre = "Dieta Test")
    {
        var dieta = new Dieta(Guid.NewGuid(), nombre, Guid.NewGuid());
        Dietas.Add(dieta);
        return dieta;
    }

    public Nutricionista AgregarNutricionista(string nombre = "Nutricionista Test")
    {
        var especialidadId = Guid.NewGuid();
        var nutricionista = new Nutricionista(Guid.NewGuid(), nombre, especialidadId);
        Nutricionistas.Add(nutricionista);
        return nutricionista;
    }

    public Menu AgregarMenu(Guid dietaId, Guid creadorId, bool esAutomatico = true, int cantidadTotalAlimentos = 2)
    {
        var menu = new Menu(
            Guid.NewGuid(),
            dietaId,
            creadorId,
            DateTime.UtcNow,
            cantidadTotalAlimentos,
            esAutomatico,
            [(TipoPreparacion.PlatoFuerte.Id, 1.0m)],
            [Guid.NewGuid()]);
        Menus.Add(menu);
        return menu;
    }
}

internal sealed class DietasFalsas(BancoDeReportesFalso banco) : IDietaRepository
{
    public Task<Dieta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Dietas.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Dieta>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Dieta>>([.. banco.Dietas]);

    public Task<Dieta?> GetConGruposYRestriccionesAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Dietas.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Dieta>> GetByProgramaAtencionAsync(Guid programaAtencionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Dieta>>([.. banco.Dietas.Where(d => d.ProgramaAtencionId == programaAtencionId)]);

    public Task AddAsync(Dieta entity, CancellationToken cancellationToken = default)
    {
        banco.Dietas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Dieta entity) => banco.Dietas.Remove(entity);
}

internal sealed class NutricionistasFalsos(BancoDeReportesFalso banco) : INutricionistaRepository
{
    public Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Nutricionistas.FirstOrDefault(n => n.Id == id));

    public Task<IReadOnlyList<Nutricionista>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([.. banco.Nutricionistas]);

    public Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([.. banco.Nutricionistas]);

    public Task AddAsync(Nutricionista entity, CancellationToken cancellationToken = default)
    {
        banco.Nutricionistas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Nutricionista entity) => banco.Nutricionistas.Remove(entity);
}

internal sealed class MenusFalsos(BancoDeReportesFalso banco) : IMenuRepository
{
    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Menus.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Menu>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus]);

    public Task AddAsync(Menu entity, CancellationToken cancellationToken = default)
    {
        banco.Menus.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Menu entity) => banco.Menus.Remove(entity);

    public Task<IReadOnlyList<Menu>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.DietaId == dietaId)]);

    public Task<IReadOnlyList<Menu>> GetByNutricionistaCreadorAsync(Guid nutricionistaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.NutricionistaCreadorId == nutricionistaId)]);

    public Task<IReadOnlyList<Menu>> GetByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([]);

    public Task<IReadOnlyList<Menu>> GetFinalesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.DietaId == dietaId && m.EsAutomatico)]);

    public Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.DietaId == dietaId && !m.EsAutomatico)]);
}
