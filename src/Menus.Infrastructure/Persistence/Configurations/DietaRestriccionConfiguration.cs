using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class DietaRestriccionConfiguration : IEntityTypeConfiguration<DietaRestriccion>
{
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
