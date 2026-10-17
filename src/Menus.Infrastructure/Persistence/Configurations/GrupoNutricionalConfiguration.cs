using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class GrupoNutricionalConfiguration : IEntityTypeConfiguration<GrupoNutricional>
{
    public void Configure(EntityTypeBuilder<GrupoNutricional> builder)
    {
        // R5-11: nombre no vacío
        builder.ToTable("grupos_nutricionales", t =>
        {
            t.HasCheckConstraint("ck_grupos_nutricionales_r5_11_nombre", "btrim(nombre) <> ''");
        });

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id");

        builder.Property(g => g.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(g => g.Nombre).IsUnique();

        builder.HasData(
            new GrupoNutricional(new Guid("11111111-0000-0000-0000-000000000001"), "Cereales"),
            new GrupoNutricional(new Guid("11111111-0000-0000-0000-000000000002"), "Proteínas"),
            new GrupoNutricional(new Guid("11111111-0000-0000-0000-000000000003"), "Lácteos"),
            new GrupoNutricional(new Guid("11111111-0000-0000-0000-000000000004"), "Frutas"),
            new GrupoNutricional(new Guid("11111111-0000-0000-0000-000000000005"), "Verduras"));
    }
}
