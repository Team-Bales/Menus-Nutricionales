using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence;

public sealed class MenusDbContext(DbContextOptions<MenusDbContext> options)
    : DbContext(options)
{
    public DbSet<GrupoNutricional> GruposNutricionales => Set<GrupoNutricional>();
    public DbSet<RestriccionAlimentaria> RestriccionesAlimentarias => Set<RestriccionAlimentaria>();
    public DbSet<ProgramaAtencion> ProgramasAtencion => Set<ProgramaAtencion>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();

    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<PlatoIngrediente> PlatosIngredientes => Set<PlatoIngrediente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenusDbContext).Assembly);
}
