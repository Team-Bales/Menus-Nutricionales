using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuAlimentoConfiguration : IEntityTypeConfiguration<MenuAlimento>
{
    public void Configure(EntityTypeBuilder<MenuAlimento> builder)
    {
        builder.ToTable("menu_alimento");

        builder.HasKey(ma => new { ma.MenuId, ma.AlimentoId });
        builder.Ignore(ma => ma.Id);

        builder.Property(ma => ma.MenuId).HasColumnName("menu_id");
        builder.Property(ma => ma.AlimentoId).HasColumnName("alimento_id");

        builder.HasOne<Alimento>()
            .WithMany()
            .HasForeignKey(ma => ma.AlimentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
