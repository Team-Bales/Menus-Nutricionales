using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ItemAlimentarioConfiguration : IEntityTypeConfiguration<ItemAlimentario>
{
    public void Configure(EntityTypeBuilder<ItemAlimentario> builder)
    {
        // Generalización Alimento/Plato en una sola tabla con discriminador (TPH). Es la traducción habitual de una
        // especialización total y disjunta cuyas subclases no tienen atributos propios: con una tabla, la totalidad
        // y la disyunción quedan declarativas (clase NOT NULL y CHECK), y alimentos y platos comparten un solo
        // espacio de identificadores y de nombres (RC-01). El costo es que una FK no puede apuntar a una sola
        // subclase, por eso RS-12 es un trigger. La clase es deducible (un plato es el ítem que tiene composición,
        // RK-03 y RS-12); se guarda como discriminador, y esas dos reglas la mantienen coherente.
        // RD-04: nombre no vacío. RK-12 y RD-17: todo ítem es de exactamente una clase (especialización total y
        // disjunta); que no la cambie (RS-13) lo comprueba Persistence/Sql/R5_19_clase_del_item.sql.
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
        // RC-01: el nombre único es el normalizado, lower(btrim(nombre)), y abarca a alimentos y platos. Ese
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
