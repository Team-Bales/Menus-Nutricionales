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

        builder.Property(p => p.TipoPreparacionId).HasColumnName("tipo_preparacion_id");

        builder.Property(p => p.NivelCaloricoId).HasColumnName("nivel_calorico_id");

        builder.Property(p => p.NutricionistaId).HasColumnName("nutricionista_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(p => p.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TipoPreparacion>()
            .WithMany()
            .HasForeignKey(p => p.TipoPreparacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NivelCalorico>()
            .WithMany()
            .HasForeignKey(p => p.NivelCaloricoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(p => p.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composición: Plato → PlatoIngrediente (CASCADE per RX-02)
        builder.HasMany(p => p.Ingredientes)
            .WithOne()
            .HasForeignKey(pi => pi.PlatoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
