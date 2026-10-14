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
    public DbSet<ResultadoNutricional> ResultadosNutricionales => Set<ResultadoNutricional>();

    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<Nutricionista> Nutricionistas => Set<Nutricionista>();
    public DbSet<NutricionistasDieta> NutricionistasDietas => Set<NutricionistasDieta>();

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<PacienteDieta> PacientesDietas => Set<PacienteDieta>();

    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<PlatoIngrediente> PlatosIngredientes => Set<PlatoIngrediente>();

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuAlimento> MenuAlimentos => Set<MenuAlimento>();
    public DbSet<MenuPlato> MenuPlatos => Set<MenuPlato>();
    public DbSet<MenuDistribucion> MenuDistribuciones => Set<MenuDistribucion>();
    public DbSet<MenuGrupoNutricional> MenuGruposNutricionales => Set<MenuGrupoNutricional>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<Validacion> Validaciones => Set<Validacion>();
    public DbSet<Consumo> Consumos => Set<Consumo>();

    public DbSet<ValoracionNutricional> ValoracionesNutricionales => Set<ValoracionNutricional>();
    public DbSet<Revalorizacion> Revalorizaciones => Set<Revalorizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenusDbContext).Assembly);
}
