using Menus.Domain.Abstractions;
using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.Nutricionistas;

/// <summary>Catálogo de nutricionistas en memoria para probar los manejadores del #71 sin EF Core (ADR-010, §9).</summary>
internal sealed class BancoDeNutricionistasFalso
{
    public static readonly Guid EspecialidadClinica = Guid.Parse("44444444-0000-0000-0000-000000000001");
    public static readonly Guid EspecialidadDeportiva = Guid.Parse("44444444-0000-0000-0000-000000000002");

    public List<Nutricionista> Nutricionistas { get; } = [];

    public UnidadDeTrabajoFalsaDeNutricionistas UnidadDeTrabajo { get; } = new();

    public NutricionistasFalsas NutricionistasRepo => new(this);

    /// <summary>Agrega un nutricionista con esa especialidad (clínica por defecto).</summary>
    public Nutricionista Agregar(string nombre = "Dra. Pérez", Guid? especialidadId = null, bool esJefe = false)
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), nombre, especialidadId ?? EspecialidadClinica);
        if (esJefe)
        {
            nutricionista.DesignarJefe();
        }

        Nutricionistas.Add(nutricionista);
        return nutricionista;
    }
}

internal sealed class NutricionistasFalsas(BancoDeNutricionistasFalso banco) : INutricionistaRepository
{
    public Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(banco.Nutricionistas.Find(n => n.Id == id));

    public Task<IReadOnlyList<Nutricionista>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([.. banco.Nutricionistas]);

    public Task AddAsync(Nutricionista entity, CancellationToken cancellationToken = default)
    {
        banco.Nutricionistas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Nutricionista entity) => banco.Nutricionistas.Remove(entity);

    public Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>(
            [.. banco.Nutricionistas.Where(n => n.DietasAutorizadas.Any(a => a.DietaId == dietaId))]);

    public Task<(IReadOnlyList<Nutricionista> Nutricionistas, int Total)> BuscarAsync(
        Guid? especialidadId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        var filtrados = banco.Nutricionistas
            .Where(n => especialidadId is null || n.EspecialidadId == especialidadId)
            .OrderBy(n => n.Nombre, StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<Nutricionista> enLaPagina = [.. filtrados.Skip((pagina - 1) * tamano).Take(tamano)];
        return Task.FromResult((enLaPagina, filtrados.Count));
    }
}

internal sealed class UnidadDeTrabajoFalsaDeNutricionistas : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }
}
