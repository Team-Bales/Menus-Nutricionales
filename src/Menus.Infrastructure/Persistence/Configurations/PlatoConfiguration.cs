using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PlatoConfiguration : IEntityTypeConfiguration<Plato>
{
    public void Configure(EntityTypeBuilder<Plato> builder)
    {
        // La tabla, las columnas y las FK son las de ItemAlimentario (TPH); el plato solo agrega su composición.
        // Composición: Plato → PlatoIngrediente (CASCADE per RX-02)
        builder.HasMany(p => p.Ingredientes)
            .WithOne()
            .HasForeignKey(pi => pi.PlatoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
