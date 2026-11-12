using Menus.Domain.Abstractions.Repositories;
using Menus.Domain.Entidades;

namespace Menus.Application.Tests.GestionDeMenus;

/// <summary>
/// Nutricionistas en memoria, para los manejadores del #99. Los menús los pone
/// <see cref="Catalogo.Dietas.BancoDeDietasFalso"/>.
/// </summary>
internal sealed class NutricionistasFalsos : INutricionistaRepository
{
    private static readonly Guid Especialidad = Guid.Parse("44444444-0000-0000-0000-000000000001");

    public List<Nutricionista> Nutricionistas { get; } = [];

    /// <summary>Agrega un nutricionista, jefe o no, autorizado para las dietas indicadas.</summary>
    public Nutricionista Agregar(bool esJefe, params Guid[] dietasAutorizadas)
        => Agregar("Nutricionista de prueba", esJefe, dietasAutorizadas);

    /// <summary>Agrega un nutricionista con ese nombre, jefe o no, autorizado para las dietas indicadas.</summary>
    public Nutricionista Agregar(string nombre, bool esJefe, params Guid[] dietasAutorizadas)
    {
        var nutricionista = new Nutricionista(Guid.NewGuid(), nombre, Especialidad);
        if (esJefe)
        {
            nutricionista.DesignarJefe();
        }

        foreach (var dieta in dietasAutorizadas)
        {
            nutricionista.AutorizarDieta(dieta);
        }

        Nutricionistas.Add(nutricionista);
        return nutricionista;
    }

    public Task<Nutricionista?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Nutricionistas.Find(n => n.Id == id));

    public Task<IReadOnlyList<Nutricionista>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>([.. Nutricionistas]);

    public Task AddAsync(Nutricionista entity, CancellationToken cancellationToken = default)
    {
        Nutricionistas.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(Nutricionista entity) => Nutricionistas.Remove(entity);

    public Task<IReadOnlyList<Nutricionista>> GetByDietaAsync(Guid dietaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Nutricionista>>(
            [.. Nutricionistas.Where(n => n.DietasAutorizadas.Any(a => a.DietaId == dietaId))]);
}
