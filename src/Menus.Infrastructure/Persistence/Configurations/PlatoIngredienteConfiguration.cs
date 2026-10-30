using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PlatoIngredienteConfiguration : IEntityTypeConfiguration<PlatoIngrediente>
{
    public void Configure(EntityTypeBuilder<PlatoIngrediente> builder)
    {
        // RD-05: cantidad estrictamente positiva
        builder.ToTable("composicion", t =>
        {
            t.HasCheckConstraint("ck_composicion_r5_12_cantidad", "cantidad > 0");
        });

        // La PK compuesta se declara con las propiedades individuales;
        // la propiedad Id (tuple) no es mapeable directamente por EF Core.
        builder.HasKey(pi => new { pi.PlatoId, pi.AlimentoId });
        builder.Ignore(pi => pi.Id);

        builder.Property(pi => pi.PlatoId).HasColumnName("plato_id");
        builder.Property(pi => pi.AlimentoId).HasColumnName("alimento_id");

        builder.Property(pi => pi.Cantidad)
            .HasColumnName("cantidad")
            .HasPrecision(10, 2)
            .IsRequired();

        // FK → Alimento (RESTRICT per RX-02; el plato no elimina sus alimentos). En la base, las dos FK apuntan a
        // items_alimentarios; que el compuesto sea un plato y el componente un alimento lo comprueba
        // Persistence/Sql/R5_18_composicion_de_un_nivel.sql.
        builder.HasOne<Alimento>()
            .WithMany()
            .HasForeignKey(pi => pi.AlimentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
