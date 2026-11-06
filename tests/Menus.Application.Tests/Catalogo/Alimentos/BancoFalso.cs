using Menus.Application.Comun;
using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;

namespace Menus.Application.Tests.Catalogo.Alimentos;

/// <summary>
/// Banco de alimentos en memoria para probar los manejadores sin EF Core (ADR-010, §9). Los repositorios falsos
/// comparten estas listas, como los de verdad comparten el contexto.
/// </summary>
internal sealed class BancoFalso
{
    public static readonly Guid Descriptor = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid GrupoCereales = Guid.Parse("11111111-0000-0000-0000-000000000001");

    public List<ItemAlimentario> Items { get; } = [];

    /// <summary>Alimentos que son ingrediente de algún plato.</summary>
    public HashSet<Guid> EnPlatos { get; } = [];

    /// <summary>Ítems que figuran en algún menú.</summary>
    public HashSet<Guid> EnMenus { get; } = [];

    public UnidadDeTrabajoFalsa UnidadDeTrabajo { get; } = new();

    public ItemsFalsos ItemsRepo => new(this);

    public AlimentosFalsos AlimentosRepo => new(this);

    public Alimento Agregar(string nombre, Guid? nivel = null, Guid? tipo = null, Guid? grupo = null)
    {
        var alimento = new Alimento(
            Guid.NewGuid(),
            nombre,
            grupo ?? GrupoCereales,
            tipo ?? TipoPreparacion.PlatoFuerte.Id,
            nivel ?? NivelCalorico.Medio.Id,
            Descriptor);
        Items.Add(alimento);
        return alimento;
    }
}

internal sealed class ItemsFalsos(BancoFalso banco) : IItemAlimentarioRepository
{
    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Items.Exists(i =>
            string.Equals(i.Nombre.Trim(), nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<bool> FiguraEnAlgunMenuAsync(Guid itemId, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnMenus.Contains(itemId));

    public Task<ItemAlimentario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Items.Find(i => i.Id == id));

    public Task<IReadOnlyList<ItemAlimentario>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ItemAlimentario>>(banco.Items);

    public Task AddAsync(ItemAlimentario entity, CancellationToken cancellationToken = default)
    {
        banco.Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(ItemAlimentario entity) => banco.Items.Remove(entity);
}

internal sealed class AlimentosFalsos(BancoFalso banco) : IAlimentoRepository
{
    private IEnumerable<Alimento> Alimentos => banco.Items.OfType<Alimento>();

    public Task<Alimento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Alimentos.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Alimento>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Alimento>>([.. Alimentos]);

    public Task AddAsync(Alimento entity, CancellationToken cancellationToken = default)
    {
        banco.Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Alimento entity) => banco.Items.Remove(entity);

    public Task<IReadOnlyList<Alimento>> GetByGrupoNutricionalAsync(Guid grupoNutricionalId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Alimento>>([.. Alimentos.Where(a => a.GrupoNutricionalId == grupoNutricionalId)]);

    public Task<IReadOnlyList<Alimento>> GetByNivelCaloricoAsync(NivelCalorico nivelCalorico, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Alimento>>([.. Alimentos.Where(a => a.NivelCaloricoId == nivelCalorico.Id)]);

    public Task<(IReadOnlyList<Alimento> Alimentos, int Total)> BuscarAsync(
        Guid? grupoNutricionalId,
        Guid? tipoPreparacionId,
        Guid? nivelCaloricoId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var filtrados = Alimentos
            .Where(a => grupoNutricionalId is null || a.GrupoNutricionalId == grupoNutricionalId)
            .Where(a => tipoPreparacionId is null || a.TipoPreparacionId == tipoPreparacionId)
            .Where(a => nivelCaloricoId is null || a.NivelCaloricoId == nivelCaloricoId)
            .OrderBy(a => a.Nombre, StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<Alimento> enPagina = [.. filtrados.Skip((pagina - 1) * tamano).Take(tamano)];
        return Task.FromResult((enPagina, filtrados.Count));
    }

    public Task<bool> FormaParteDeAlgunPlatoAsync(Guid alimentoId, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.EnPlatos.Contains(alimentoId));
}

internal sealed class UnidadDeTrabajoFalsa : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}

internal sealed class ActorFalso(Guid id) : IActorActual
{
    public Guid Id { get; } = id;
}
