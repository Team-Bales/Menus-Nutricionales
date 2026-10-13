using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("pacientes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Edad)
            .HasColumnName("edad")
            .IsRequired();

        builder.Property(p => p.SalaId).HasColumnName("sala_id");

        builder.HasOne<Sala>()
            .WithMany()
            .HasForeignKey(p => p.SalaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inscribe: Paciente → PacienteDieta (Cascade: si se elimina el paciente, se eliminan sus inscripciones)
        builder.HasMany(p => p.Dietas)
            .WithOne()
            .HasForeignKey(pd => pd.PacienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
