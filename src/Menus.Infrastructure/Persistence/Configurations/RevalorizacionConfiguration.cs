using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class RevalorizacionConfiguration : IEntityTypeConfiguration<Revalorizacion>
{
    public void Configure(EntityTypeBuilder<Revalorizacion> builder)
    {
        builder.ToTable("revalorizaciones");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.FechaSolicitud)
            .HasColumnName("fecha_solicitud")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.PacienteId).HasColumnName("paciente_id");
        builder.Property(r => r.NutricionistaId).HasColumnName("nutricionista_id");
        builder.Property(r => r.ValoracionRevisadaId).HasColumnName("valoracion_revisada_id");
        builder.Property(r => r.ValoracionGeneradaId).HasColumnName("valoracion_generada_id");

        builder.HasOne<Paciente>()
            .WithMany()
            .HasForeignKey(r => r.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(r => r.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Revalora: una valoración puede revisarse varias veces
        builder.HasOne<ValoracionNutricional>()
            .WithMany()
            .HasForeignKey(r => r.ValoracionRevisadaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Genera: (0,1) en los dos lados, así que la FK es única y admite nulos (R7-24)
        builder.HasOne<ValoracionNutricional>()
            .WithOne()
            .HasForeignKey<Revalorizacion>(r => r.ValoracionGeneradaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
