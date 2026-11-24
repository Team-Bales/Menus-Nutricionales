using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="PacienteDieta"/> (relación <c>Inscribe</c>) a la tabla <c>paciente_dieta</c>: las
/// dietas en las que está inscripto cada paciente.
/// </summary>
internal sealed class PacienteDietaConfiguration : IEntityTypeConfiguration<PacienteDieta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PacienteDieta> builder)
    {
        builder.ToTable("paciente_dieta");

        builder.HasKey(pd => new { pd.PacienteId, pd.DietaId });
        builder.Ignore(pd => pd.Id);

        builder.Property(pd => pd.PacienteId).HasColumnName("paciente_id");
        builder.Property(pd => pd.DietaId).HasColumnName("dieta_id");

        builder.HasOne<Dieta>()
            .WithMany()
            .HasForeignKey(pd => pd.DietaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
