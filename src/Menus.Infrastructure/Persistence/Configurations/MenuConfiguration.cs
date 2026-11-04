using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // RD-10: cantidad total de alimentos estrictamente positiva. RS-19: un menú no es reemplazo de sí mismo.
        builder.ToTable("menus", t =>
        {
            t.HasCheckConstraint("ck_menus_r7_15_cantidad_total_alimentos", "cantidad_total_alimentos > 0");
            t.HasCheckConstraint("ck_menus_r7_37_validacion_origen", "validacion_origen_id <> id");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.DietaId).HasColumnName("dieta_id");

        builder.HasOne<Dieta>()
            .WithMany()
            .HasForeignKey(m => m.DietaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.NutricionistaCreadorId).HasColumnName("nutricionista_creador_id");

        builder.Property(m => m.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.CantidadTotalAlimentos)
            .HasColumnName("cantidad_total_alimentos")
            .IsRequired();

        builder.Property(m => m.EsAutomatico)
            .HasColumnName("es_automatico")
            .IsRequired();

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(m => m.NutricionistaCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade solo a la parametrización del menú: Incluye, Distribución y Cubre (RX-02)
        builder.HasMany(m => m.Items)
            .WithOne()
            .HasForeignKey(mi => mi.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Distribuciones)
            .WithOne()
            .HasForeignKey(md => md.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GruposNutricionales)
            .WithOne()
            .HasForeignKey(mg => mg.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: no se elimina un menú asignado o validado (RR-19, RX-02)
        builder.HasMany(m => m.Asignaciones)
            .WithOne()
            .HasForeignKey(a => a.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un menú tiene a lo sumo una validación (RC-12 y RK-13): la relación es 1 a 0..1 y la FK es la clave
        // de Validacion
        builder.HasOne(m => m.Validacion)
            .WithOne()
            .HasForeignKey<Validacion>(v => v.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        // Se carga siempre con el menú: EsPendiente, EsFinal y EsRechazado se derivan de ella, y sin cargarla un
        // menú con dictamen parecería pendiente (revisión del 19/10). Cuesta un LEFT JOIN por la clave.
        builder.Navigation(m => m.Validacion).AutoInclude();

        // Origina (#155, P1-5): la validación rechazada origina a lo sumo un menú de reemplazo, y un menú reemplaza
        // a lo sumo una validación, (0,1) en los dos lados (RK-15). Por eso la FK está en el menú, admite nulos y
        // es única. RESTRICT, como todas las FK que no van del dueño a sus propias filas (RX-02). Que el origen sea
        // un rechazo y de la misma dieta (RS-17 y RS-18) lo comprueba
        // Persistence/Sql/R7_35_R7_36_menu_de_reemplazo.sql.
        builder.Property(m => m.ValidacionOrigenId).HasColumnName("validacion_origen_id");

        builder.HasOne<Validacion>()
            .WithOne()
            .HasForeignKey<Menu>(m => m.ValidacionOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ValidacionOrigenId)
            .IsUnique()
            .HasDatabaseName("ux_menus_r7_38_validacion_origen");
    }
}
