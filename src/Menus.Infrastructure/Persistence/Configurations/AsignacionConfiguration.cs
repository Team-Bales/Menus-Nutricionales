using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class AsignacionConfiguration : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> builder)
    {
        builder.ToTable("asignacion");

        builder.HasKey(a => new { a.MenuId, a.PacienteId });
        builder.Ignore(a => a.Id);

        builder.Property(a => a.MenuId).HasColumnName("menu_id");
        builder.Property(a => a.PacienteId).HasColumnName("paciente_id");

        builder.HasOne<Paciente>()
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
