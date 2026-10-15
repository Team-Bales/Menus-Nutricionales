using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class TipoPreparacionConfiguration : IEntityTypeConfiguration<TipoPreparacion>
{
    public void Configure(EntityTypeBuilder<TipoPreparacion> builder)
    {
        builder.ToTable("tipos_preparacion");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Nombre).IsUnique();

        builder.HasData(TipoPreparacion.Todos);
    }
}
