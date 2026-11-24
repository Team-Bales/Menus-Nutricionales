using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="DietaRestriccion"/> (relación <c>Restringe</c>) a la tabla <c>restringe</c>: las
/// restricciones alimentarias de cada dieta.
/// </summary>
internal sealed class DietaRestriccionConfiguration : IEntityTypeConfiguration<DietaRestriccion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DietaRestriccion> builder)
    {
        builder.ToTable("restringe");

        builder.HasKey(dr => new { dr.DietaId, dr.RestriccionAlimentariaId });
        builder.Ignore(dr => dr.Id);

        builder.Property(dr => dr.DietaId).HasColumnName("dieta_id");
        builder.Property(dr => dr.RestriccionAlimentariaId).HasColumnName("restriccion_alimentaria_id");

        // FK → Dieta se configura desde DietaConfiguration (colección Restricciones)
        builder.HasOne<RestriccionAlimentaria>()
            .WithMany()
            .HasForeignKey(dr => dr.RestriccionAlimentariaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
