using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ItemAlimentarioConfiguration : IEntityTypeConfiguration<ItemAlimentario>
{
    public void Configure(EntityTypeBuilder<ItemAlimentario> builder)
    {
        // Generalización Alimento/Plato en una sola tabla con discriminador (TPH). Las subclases no tienen
        // atributos propios, así que separarlas en tablas solo repetiría columnas; y es el esquema que da la
        // síntesis: una sola dependencia id → nombre, clase y clasificación.
        // R5-11: nombre no vacío. R5-19: todo ítem es de exactamente una clase (especialización total y
        // disjunta); que no la cambie lo comprueba Persistence/Sql/R5_19_clase_del_item.sql.
        builder.ToTable("items_alimentarios", t =>
        {
            t.HasCheckConstraint("ck_items_alimentarios_r5_11_nombre", "btrim(nombre) <> ''");
            t.HasCheckConstraint("ck_items_alimentarios_r5_19_clase", "clase IN ('Alimento', 'Plato')");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.HasDiscriminator<string>("Clase")
            .HasValue<Alimento>("Alimento")
            .HasValue<Plato>("Plato");

        builder.Property<string>("Clase")
            .HasColumnName("clase")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        // R5-16: el nombre único es el normalizado, lower(btrim(nombre)), y abarca a alimentos y platos. Ese
        // índice está en Persistence/Sql/R5_16_nombre_normalizado.sql, porque EF no declara índices sobre
        // expresiones.

        builder.Property(i => i.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        builder.Property(i => i.TipoPreparacionId).HasColumnName("tipo_preparacion_id");

        builder.Property(i => i.NivelCaloricoId).HasColumnName("nivel_calorico_id");

        builder.Property(i => i.NutricionistaId).HasColumnName("nutricionista_id");

        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(i => i.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TipoPreparacion>()
            .WithMany()
            .HasForeignKey(i => i.TipoPreparacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NivelCalorico>()
            .WithMany()
            .HasForeignKey(i => i.NivelCaloricoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(i => i.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
