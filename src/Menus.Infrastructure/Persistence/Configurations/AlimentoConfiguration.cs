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

        builder.Property(a => a.TipoPreparacionId).HasColumnName("tipo_preparacion_id");

        builder.Property(a => a.NivelCaloricoId).HasColumnName("nivel_calorico_id");

        builder.Property(a => a.NutricionistaId).HasColumnName("nutricionista_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(a => a.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TipoPreparacion>()
            .WithMany()
            .HasForeignKey(a => a.TipoPreparacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NivelCalorico>()
            .WithMany()
            .HasForeignKey(a => a.NivelCaloricoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(a => a.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
