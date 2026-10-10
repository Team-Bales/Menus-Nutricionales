using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class RestriccionAlimentariaConfiguration : IEntityTypeConfiguration<RestriccionAlimentaria>
{
    public void Configure(EntityTypeBuilder<RestriccionAlimentaria> builder)
    {
        builder.ToTable("restricciones_alimentarias");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(r => r.Nombre).IsUnique();
    }
}
