using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PlatoConfiguration : IEntityTypeConfiguration<Plato>
{
    public void Configure(EntityTypeBuilder<Plato> builder)
    {
        builder.ToTable("platos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(p => p.Nombre).IsUnique();

        builder.Property(p => p.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        builder.Property(p => p.TipoPreparacion)
            .HasColumnName("tipo_preparacion")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.NivelCalorico)
            .HasColumnName("nivel_calorico")
            .HasConversion<string>()
            .IsRequired();

        // FK → Nutricionista se configura cuando esa entidad se incorpore (issue #52)
        builder.Property(p => p.NutricionistaId).HasColumnName("nutricionista_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(p => p.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composición: Plato → PlatoIngrediente (CASCADE per RX-02)
        builder.HasMany(p => p.Ingredientes)
            .WithOne()
            .HasForeignKey(pi => pi.PlatoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
