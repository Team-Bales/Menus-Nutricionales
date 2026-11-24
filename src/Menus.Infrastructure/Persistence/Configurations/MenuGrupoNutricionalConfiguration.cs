using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="MenuGrupoNutricional"/> (relación <c>Cubre</c>) a la tabla
/// <c>menu_grupo_nutricional</c>: los grupos nutricionales que cubre un menú.
/// </summary>
internal sealed class MenuGrupoNutricionalConfiguration : IEntityTypeConfiguration<MenuGrupoNutricional>
{
    /// <inheritdoc />
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
