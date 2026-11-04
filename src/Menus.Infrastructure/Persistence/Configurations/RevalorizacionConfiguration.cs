using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class RevalorizacionConfiguration : IEntityTypeConfiguration<Revalorizacion>
{
    public void Configure(EntityTypeBuilder<Revalorizacion> builder)
    {
        // Sin el paciente solicitante: se deduce de la valoración revisada (RS-09), y guardarlo dejaba la tabla
        // fuera de la 3FN por una dependencia transitiva (#12, §2.3; revisión del 19/10).
        builder.ToTable("revalorizaciones");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.FechaSolicitud)
            .HasColumnName("fecha_solicitud")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.NutricionistaId).HasColumnName("nutricionista_id");
        builder.Property(r => r.ValoracionRevisadaId).HasColumnName("valoracion_revisada_id");
        builder.Property(r => r.ValoracionGeneradaId).HasColumnName("valoracion_generada_id");

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(r => r.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Revalora: una valoración puede revisarse varias veces
        builder.HasOne<ValoracionNutricional>()
            .WithMany()
            .HasForeignKey(r => r.ValoracionRevisadaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Genera: (0,1) en los dos lados, así que la FK es única y admite nulos (RK-11)
        builder.HasOne<ValoracionNutricional>()
            .WithOne()
            .HasForeignKey<Revalorizacion>(r => r.ValoracionGeneradaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
