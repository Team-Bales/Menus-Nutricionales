using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class NivelCaloricoConfiguration : IEntityTypeConfiguration<NivelCalorico>
{
    public void Configure(EntityTypeBuilder<NivelCalorico> builder)
    {
        builder.ToTable("niveles_caloricos");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(n => n.Nombre).IsUnique();

        builder.HasData(NivelCalorico.Todos);
    }
}
