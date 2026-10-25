using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        // Incluye(Menú, ItemAlimentario): una sola relación para alimentos y platos (R7-07: sin repetidos)
        builder.ToTable("menu_item");

        builder.HasKey(mi => new { mi.MenuId, mi.ItemId });
        builder.Ignore(mi => mi.Id);

        builder.Property(mi => mi.MenuId).HasColumnName("menu_id");
        builder.Property(mi => mi.ItemId).HasColumnName("item_id");

        // FK → Menu se configura desde MenuConfiguration (colección Items, CASCADE per RX-02)
        builder.HasOne<ItemAlimentario>()
            .WithMany()
            .HasForeignKey(mi => mi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
