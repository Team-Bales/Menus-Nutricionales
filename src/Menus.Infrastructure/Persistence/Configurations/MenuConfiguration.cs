using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // R7-15: cantidad total de alimentos estrictamente positiva
        builder.ToTable("menus", t =>
        {
            t.HasCheckConstraint("ck_menus_r7_15_cantidad_total_alimentos", "cantidad_total_alimentos > 0");
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

        // Cascade solo a la parametrización del menú: Incluye, Distribución y Cubre (R11-02)
        builder.HasMany(m => m.Alimentos)
            .WithOne()
            .HasForeignKey(ma => ma.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Platos)
            .WithOne()
            .HasForeignKey(mp => mp.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Distribuciones)
            .WithOne()
            .HasForeignKey(md => md.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.GruposNutricionales)
            .WithOne()
            .HasForeignKey(mg => mg.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: no se elimina un menú asignado o validado (R7-14, R11-02)
        builder.HasMany(m => m.Asignaciones)
            .WithOne()
            .HasForeignKey(a => a.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Validaciones)
            .WithOne()
            .HasForeignKey(v => v.MenuId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
