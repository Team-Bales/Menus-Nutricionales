using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="NutricionistasDieta"/> (relación <c>Autoriza</c>) a la tabla <c>autoriza</c>:
/// las dietas en las que cada nutricionista puede crear y validar menús.
/// </summary>
internal sealed class NutricionistasDietaConfiguration : IEntityTypeConfiguration<NutricionistasDieta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NutricionistasDieta> builder)
    {
        builder.ToTable("autoriza");

        builder.HasKey(nd => new { nd.NutricionistaId, nd.DietaId });
        builder.Ignore(nd => nd.Id);

        builder.Property(nd => nd.NutricionistaId).HasColumnName("nutricionista_id");
        builder.Property(nd => nd.DietaId).HasColumnName("dieta_id");

        builder.HasOne<Dieta>()
            .WithMany()
            .HasForeignKey(nd => nd.DietaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
