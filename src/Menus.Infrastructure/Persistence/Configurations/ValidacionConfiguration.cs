using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ValidacionConfiguration : IEntityTypeConfiguration<Validacion>
{
    public void Configure(EntityTypeBuilder<Validacion> builder)
    {
        builder.ToTable("validacion");

        // R7-32: un menú tiene a lo sumo una validación, así que el menú es la clave. El par (revisor, menú) ya no
        // sería mínimo. La propiedad Id del dominio es la misma que MenuId y no se mapea aparte.
        builder.HasKey(v => v.MenuId);
        builder.Ignore(v => v.Id);

        builder.Property(v => v.MenuId).HasColumnName("menu_id");
        builder.Property(v => v.NutricionistaId).HasColumnName("nutricionista_id");

        builder.Property(v => v.FechaValidacion)
            .HasColumnName("fecha_validacion")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(v => v.Observaciones).HasColumnName("observaciones");

        // FK → Menu se configura desde MenuConfiguration (navegación Validacion)
        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(v => v.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
