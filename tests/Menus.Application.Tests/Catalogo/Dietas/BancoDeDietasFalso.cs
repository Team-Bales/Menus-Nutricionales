using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Dietas;

/// <summary>
/// Catálogo de dietas en memoria para probar los manejadores del #63 sin EF Core (ADR-010, §9), igual que
/// <see cref="Alimentos.BancoFalso"/>.
/// </summary>
internal sealed class BancoDeDietasFalso
{
    public static readonly Guid ProgramaPediatria = Guid.Parse("33333333-0000-0000-0000-000000000001");
    public static readonly Guid ProgramaOncologia = Guid.Parse("33333333-0000-0000-0000-000000000002");
    public static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid GrupoFrutas = Guid.Parse("11111111-0000-0000-0000-000000000004");
    public static readonly Guid RestriccionSinGluten = Guid.Parse("22222222-0000-0000-0000-000000000001");

    public List<Dieta> Dietas { get; } = [];

    public List<Paciente> Pacientes { get; } = [];

    public List<Menu> Menus { get; } = [];

    /// <summary>Grupos que excluye cada restricción alimentaria (#101: <see cref="DietasFalsas.GetGruposProhibidosAsync"/>).</summary>
    public Dictionary<Guid, List<Guid>> GruposExcluidosPorRestriccion { get; } = [];

    /// <summary>Nivel calórico de cada ítem del banco (#101: <see cref="MenusFalsos.ContarItemsFinalesPorNivelCaloricoAsync"/>).</summary>
    public Dictionary<Guid, Guid> NivelesDeItems { get; } = [];

    public UnidadDeTrabajoFalsaDeDietas UnidadDeTrabajo { get; } = new();

    public DietasFalsas DietasRepo => new(this);

    public PacientesFalsos PacientesRepo => new(this);

    public MenusFalsos MenusRepo => new(this);

    /// <summary>Registra que una restricción alimentaria excluye un grupo nutricional.</summary>
    public void ExcluirGrupo(Guid restriccionAlimentariaId, Guid grupoNutricionalId)
    {
        if (!GruposExcluidosPorRestriccion.TryGetValue(restriccionAlimentariaId, out var grupos))
        {
            grupos = [];
            GruposExcluidosPorRestriccion[restriccionAlimentariaId] = grupos;
        }

        grupos.Add(grupoNutricionalId);
    }

    /// <summary>Registra el nivel calórico de un ítem del banco, para contarlo en los menús finales.</summary>
    public void RegistrarNivelDeItem(Guid itemId, Guid nivelCaloricoId) => NivelesDeItems[itemId] = nivelCaloricoId;

    /// <summary>Agrega una dieta con un grupo requerido, para que ya cumpla RK-07.</summary>
    public Dieta Agregar(string nombre, Guid? programa = null, Guid? grupoRequerido = null)
    {
        var dieta = new Dieta(Guid.NewGuid(), nombre, programa ?? ProgramaPediatria);
        dieta.AgregarGrupo(grupoRequerido ?? GrupoCereales, TipoCobertura.Requerido);
        Dietas.Add(dieta);
        return dieta;
    }

    /// <summary>Inscribe un paciente nuevo en la dieta, para que <c>GetByDietaAsync</c> la encuentre en uso.</summary>
    public void InscribirPaciente(Guid dietaId)
    {
        var paciente = new Paciente(Guid.NewGuid(), "Paciente Falso", 30, Guid.NewGuid());
        paciente.InscribirDieta(dietaId);
        Pacientes.Add(paciente);
    }

    /// <summary>Crea un menú de la dieta, para que <c>GetByDietaAsync</c> la encuentre en uso.</summary>
    public void CrearMenu(Guid dietaId)
    {
        var menu = new Menu(
            Guid.NewGuid(), dietaId, Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), 1, true,
            [(TipoPreparacion.PlatoFuerte.Id, 1m)], [GrupoCereales]);
        Menus.Add(menu);
    }
}

internal sealed class DietasFalsas(BancoDeDietasFalso banco) : IDietaRepository
{
    public Task<Dieta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Dietas.Find(d => d.Id == id));

    public Task<IReadOnlyList<Dieta>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Dieta>>([.. banco.Dietas]);

    public Task AddAsync(Dieta entity, CancellationToken cancellationToken = default)
    {
        banco.Dietas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Dieta entity) => banco.Dietas.Remove(entity);

    public Task<Dieta?> GetConGruposYRestriccionesAsync(Guid id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Dieta>> GetByProgramaAtencionAsync(Guid programaAtencionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Dieta>>([.. banco.Dietas.Where(d => d.ProgramaAtencionId == programaAtencionId)]);

    public Task<(IReadOnlyList<Dieta> Dietas, int Total)> BuscarAsync(
        Guid? programaAtencionId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var filtradas = banco.Dietas
            .Where(d => programaAtencionId is null || d.ProgramaAtencionId == programaAtencionId)
            .OrderBy(d => d.Nombre, StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<Dieta> enPagina = [.. filtradas.Skip((pagina - 1) * tamano).Take(tamano)];
        return Task.FromResult((enPagina, filtradas.Count));
    }

    public Task<IReadOnlyList<Dieta>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Dieta>>([.. banco.Dietas.Where(d => ids.Contains(d.Id))]);

    public Task<IReadOnlyList<Guid>> GetGruposProhibidosAsync(Guid dietaId, CancellationToken cancellationToken = default)
    {
        var dieta = banco.Dietas.Find(d => d.Id == dietaId);
        if (dieta is null)
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        var restringidos = dieta.GruposNutricionales
            .Where(g => g.Tipo == TipoCobertura.Restringido)
            .Select(g => g.GrupoNutricionalId);

        var excluidos = dieta.Restricciones
            .SelectMany(r => banco.GruposExcluidosPorRestriccion.GetValueOrDefault(r.RestriccionAlimentariaId, []));

        return Task.FromResult<IReadOnlyList<Guid>>([.. restringidos.Union(excluidos)]);
    }
}

internal sealed class PacientesFalsos(BancoDeDietasFalso banco) : IPacienteRepository
{
    public Task<Paciente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Pacientes.Find(p => p.Id == id));

    public Task<IReadOnlyList<Paciente>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Paciente>>([.. banco.Pacientes]);

    public Task AddAsync(Paciente entity, CancellationToken cancellationToken = default)
    {
        banco.Pacientes.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Paciente entity) => banco.Pacientes.Remove(entity);

    public Task<IReadOnlyList<Paciente>> GetBySalaAsync(Guid salaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Paciente>>([.. banco.Pacientes.Where(p => p.SalaId == salaId)]);

    public Task<IReadOnlyList<Paciente>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Paciente>>([.. banco.Pacientes.Where(p => p.Dietas.Any(d => d.DietaId == dietaId))]);

    public Task<(IReadOnlyList<Paciente> Pacientes, int Total)> BuscarAsync(
        Guid? salaId,
        Guid? dietaId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var filtrados = banco.Pacientes
            .Where(p => salaId is null || p.SalaId == salaId)
            .Where(p => dietaId is null || p.Dietas.Any(d => d.DietaId == dietaId))
            .OrderBy(p => p.Nombre, StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<Paciente> enPagina = [.. filtrados.Skip((pagina - 1) * tamano).Take(tamano)];
        return Task.FromResult((enPagina, filtrados.Count));
    }
}

internal sealed class MenusFalsos(BancoDeDietasFalso banco) : IMenuRepository
{
    public Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Menus.Find(m => m.Id == id));

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
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.DietaId == dietaId && m.EsFinal)]);

    public Task<IReadOnlyList<Menu>> GetPendientesByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Menu>>([.. banco.Menus.Where(m => m.DietaId == dietaId && m.EsPendiente)]);

    public Task<IReadOnlyDictionary<Guid, int>> ContarItemsFinalesPorNivelCaloricoAsync(Guid dietaId, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, int> conteo = banco.Menus
            .Where(m => m.DietaId == dietaId && m.EsFinal)
            .SelectMany(m => m.Items)
            .Select(i => banco.NivelesDeItems[i.ItemId])
            .GroupBy(nivelId => nivelId)
            .ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult(conteo);
    }
}

internal sealed class UnidadDeTrabajoFalsaDeDietas : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}
