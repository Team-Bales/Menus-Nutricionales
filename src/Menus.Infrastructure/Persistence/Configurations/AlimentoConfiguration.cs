using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class AlimentoConfiguration : IEntityTypeConfiguration<Alimento>
{
    public void Configure(EntityTypeBuilder<Alimento> builder)
    {
        builder.ToTable("alimentos");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(a => a.Nombre).IsUnique();

        builder.Property(a => a.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        builder.Property(a => a.TipoPreparacion)
            .HasColumnName("tipo_preparacion")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.NivelCalorico)
            .HasColumnName("nivel_calorico")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.NutricionistaId).HasColumnName("nutricionista_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(a => a.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(a => a.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
