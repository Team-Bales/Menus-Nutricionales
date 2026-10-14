using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class DietaGrupoNutricionalConfiguration : IEntityTypeConfiguration<DietaGrupoNutricional>
{
    public void Configure(EntityTypeBuilder<DietaGrupoNutricional> builder)
    {
        builder.ToTable("cubre_dieta");

        builder.HasKey(dg => new { dg.DietaId, dg.GrupoNutricionalId });
        builder.Ignore(dg => dg.Id);

        builder.Property(dg => dg.DietaId).HasColumnName("dieta_id");
        builder.Property(dg => dg.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        builder.Property(dg => dg.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .IsRequired();

        // FK → Dieta se configura desde DietaConfiguration (colección GruposNutricionales)
        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(dg => dg.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
