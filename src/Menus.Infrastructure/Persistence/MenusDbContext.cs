using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;

namespace Menus.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core sobre la base relacional (PostgreSQL). Cada tabla tiene su configuración en
/// <c>Persistence/Configurations</c>; las restricciones que EF no puede declarar (triggers e índices sobre
/// expresiones) están en <c>Persistence/Sql</c> y las aplica la migración.
/// </summary>
/// <param name="options">Opciones del contexto (proveedor y cadena de conexión).</param>
public sealed class MenusDbContext(DbContextOptions<MenusDbContext> options)
    : DbContext(options)
{
    /// <summary>Nomenclador de grupos nutricionales.</summary>
    public DbSet<GrupoNutricional> GruposNutricionales => Set<GrupoNutricional>();

    /// <summary>Nomenclador de restricciones alimentarias.</summary>
    public DbSet<RestriccionAlimentaria> RestriccionesAlimentarias => Set<RestriccionAlimentaria>();

    /// <summary>Grupos nutricionales que excluye cada restricción alimentaria (relación <c>Excluye</c>).</summary>
    public DbSet<RestriccionGrupoExcluido> RestriccionesGruposExcluidos => Set<RestriccionGrupoExcluido>();

    /// <summary>Nomenclador de programas de atención.</summary>
    public DbSet<ProgramaAtencion> ProgramasAtencion => Set<ProgramaAtencion>();

    /// <summary>Nomenclador de especialidades.</summary>
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();

    /// <summary>Catálogo cerrado de resultados nutricionales.</summary>
    public DbSet<ResultadoNutricional> ResultadosNutricionales => Set<ResultadoNutricional>();

    /// <summary>Catálogo cerrado de tipos de preparación.</summary>
    public DbSet<TipoPreparacion> TiposPreparacion => Set<TipoPreparacion>();

    /// <summary>Catálogo cerrado de niveles calóricos.</summary>
    public DbSet<NivelCalorico> NivelesCaloricos => Set<NivelCalorico>();

    /// <summary>Dietas.</summary>
    public DbSet<Dieta> Dietas => Set<Dieta>();

    /// <summary>Grupos nutricionales que cubre cada dieta, con su tipo (relación <c>Cubre</c>).</summary>
    public DbSet<DietaGrupoNutricional> DietasGruposNutricionales => Set<DietaGrupoNutricional>();

    /// <summary>Restricciones alimentarias de cada dieta (relación <c>Restringe</c>).</summary>
    public DbSet<DietaRestriccion> DietasRestricciones => Set<DietaRestriccion>();

    /// <summary>Salas de hospitalización.</summary>
    public DbSet<Sala> Salas => Set<Sala>();

    /// <summary>Nutricionistas.</summary>
    public DbSet<Nutricionista> Nutricionistas => Set<Nutricionista>();

    /// <summary>Dietas para las que está autorizado cada nutricionista (relación <c>Autoriza</c>).</summary>
    public DbSet<NutricionistasDieta> NutricionistasDietas => Set<NutricionistasDieta>();

    /// <summary>Pacientes.</summary>
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    /// <summary>Dietas en las que está inscrito cada paciente (relación <c>Inscribe</c>).</summary>
    public DbSet<PacienteDieta> PacientesDietas => Set<PacienteDieta>();

    /// <summary>Todos los ítems del banco, alimentos y platos (generalización en una sola tabla).</summary>
    public DbSet<ItemAlimentario> ItemsAlimentarios => Set<ItemAlimentario>();

    /// <summary>Los ítems de clase <see cref="Alimento"/>.</summary>
    public DbSet<Alimento> Alimentos => Set<Alimento>();

    /// <summary>Los ítems de clase <see cref="Plato"/>.</summary>
    public DbSet<Plato> Platos => Set<Plato>();

    /// <summary>Composición de los platos (relación <c>Compone</c>).</summary>
    public DbSet<PlatoIngrediente> PlatosIngredientes => Set<PlatoIngrediente>();

    /// <summary>Menús.</summary>
    public DbSet<Menu> Menus => Set<Menu>();

    /// <summary>Ítems incluidos en cada menú (relación <c>Incluye</c>).</summary>
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    /// <summary>Proporción por tipo de preparación de cada menú (relación <c>Distribuye</c>).</summary>
    public DbSet<MenuDistribucion> MenuDistribuciones => Set<MenuDistribucion>();

    /// <summary>Grupos nutricionales que cubre cada menú (relación <c>Cubre</c> del menú).</summary>
    public DbSet<MenuGrupoNutricional> MenuGruposNutricionales => Set<MenuGrupoNutricional>();

    /// <summary>Asignaciones de menús a pacientes (agregación <c>Asignación</c>).</summary>
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();

    /// <summary>Días en que se sirvió cada asignación (entidad débil <c>Servicio</c>).</summary>
    public DbSet<Servicio> Servicios => Set<Servicio>();

    /// <summary>Aprobaciones de los menús, a lo sumo una por menú (relación <c>Valida</c>).</summary>
    public DbSet<Validacion> Validaciones => Set<Validacion>();

    /// <summary>Consumos de cada ítem en cada servicio (relación <c>Consume</c>).</summary>
    public DbSet<Consumo> Consumos => Set<Consumo>();

    /// <summary>Valoraciones nutricionales.</summary>
    public DbSet<ValoracionNutricional> ValoracionesNutricionales => Set<ValoracionNutricional>();

    /// <summary>Revalorizaciones solicitadas.</summary>
    public DbSet<Revalorizacion> Revalorizaciones => Set<Revalorizacion>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenusDbContext).Assembly);
}
