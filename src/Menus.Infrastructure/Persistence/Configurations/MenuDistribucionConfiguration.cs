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

        builder.HasKey(md => new { md.MenuId, md.TipoPreparacionId });
        builder.Ignore(md => md.Id);

        builder.Property(md => md.MenuId).HasColumnName("menu_id");

        builder.Property(md => md.TipoPreparacionId).HasColumnName("tipo_preparacion_id");

        builder.HasOne<TipoPreparacion>()
            .WithMany()
            .HasForeignKey(md => md.TipoPreparacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(md => md.Proporcion)
            .HasColumnName("proporcion")
            .HasPrecision(5, 4)
            .IsRequired();
    }
}
