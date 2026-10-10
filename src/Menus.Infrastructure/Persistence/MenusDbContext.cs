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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenusDbContext).Assembly);
}
