using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class GrupoNutricionalConfiguration : IEntityTypeConfiguration<GrupoNutricional>
{
    public void Configure(EntityTypeBuilder<GrupoNutricional> builder)
    {
        builder.ToTable("grupos_nutricionales");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(g => g.Nombre).IsUnique();
    }
}
