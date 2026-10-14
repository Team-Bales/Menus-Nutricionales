using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuPlatoConfiguration : IEntityTypeConfiguration<MenuPlato>
{
    public void Configure(EntityTypeBuilder<MenuPlato> builder)
    {
        builder.ToTable("menu_plato");

        builder.HasKey(mp => new { mp.MenuId, mp.PlatoId });
        builder.Ignore(mp => mp.Id);

        builder.Property(mp => mp.MenuId).HasColumnName("menu_id");
        builder.Property(mp => mp.PlatoId).HasColumnName("plato_id");

        builder.HasOne<Plato>()
            .WithMany()
            .HasForeignKey(mp => mp.PlatoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
