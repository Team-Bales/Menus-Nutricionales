using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        // RD-07: nombre no vacío; RD-06: edad mayor o igual que cero
        builder.ToTable("pacientes", t =>
        {
            t.HasCheckConstraint("ck_pacientes_r6_15_nombre", "btrim(nombre) <> ''");
            t.HasCheckConstraint("ck_pacientes_r6_14_edad", "edad >= 0");
        });

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

        // Inscribe: Paciente → PacienteDieta (Restrict: no se elimina un paciente con inscripciones, RX-02)
        builder.HasMany(p => p.Dietas)
            .WithOne()
            .HasForeignKey(pd => pd.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
