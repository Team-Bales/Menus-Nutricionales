using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class DietaConfiguration : IEntityTypeConfiguration<Dieta>
{
    public void Configure(EntityTypeBuilder<Dieta> builder)
    {
        builder.ToTable("dietas");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.ProgramaAtencionId).HasColumnName("programa_atencion_id");

        builder.HasOne<ProgramaAtencion>()
            .WithMany()
            .HasForeignKey(d => d.ProgramaAtencionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade: Dieta → CubreDieta y Restringe (R11-02)
        builder.HasMany(d => d.GruposNutricionales)
            .WithOne()
            .HasForeignKey(dg => dg.DietaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Restricciones)
            .WithOne()
            .HasForeignKey(dr => dr.DietaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
