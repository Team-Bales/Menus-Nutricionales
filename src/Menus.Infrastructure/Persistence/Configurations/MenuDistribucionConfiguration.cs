using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuDistribucionConfiguration : IEntityTypeConfiguration<MenuDistribucion>
{
    public void Configure(EntityTypeBuilder<MenuDistribucion> builder)
    {
        builder.ToTable("menu_distribucion");

        builder.HasKey(md => new { md.MenuId, md.TipoPreparacion });
        builder.Ignore(md => md.Id);

        builder.Property(md => md.MenuId).HasColumnName("menu_id");

        builder.Property(md => md.TipoPreparacion)
            .HasColumnName("tipo_preparacion")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(md => md.Proporcion)
            .HasColumnName("proporcion")
            .HasPrecision(5, 4)
            .IsRequired();
    }
}
