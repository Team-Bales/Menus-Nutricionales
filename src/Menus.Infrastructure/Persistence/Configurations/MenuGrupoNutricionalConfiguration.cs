using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class MenuGrupoNutricionalConfiguration : IEntityTypeConfiguration<MenuGrupoNutricional>
{
    public void Configure(EntityTypeBuilder<MenuGrupoNutricional> builder)
    {
        builder.ToTable("menu_grupo_nutricional");

        builder.HasKey(mg => new { mg.MenuId, mg.GrupoNutricionalId });
        builder.Ignore(mg => mg.Id);

        builder.Property(mg => mg.MenuId).HasColumnName("menu_id");
        builder.Property(mg => mg.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(mg => mg.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
