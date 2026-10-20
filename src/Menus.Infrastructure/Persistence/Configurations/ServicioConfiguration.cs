using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> builder)
    {
        // Entidad débil de Asignación: su clave es la de la asignación más la fecha del servicio. Que el menú
        // esté aprobado y el servicio no sea anterior a la aprobación (R7-31) lo comprueba
        // Persistence/Sql/R7_31_servicio_tras_aprobacion.sql.
        builder.ToTable("servicios");

        builder.HasKey(s => new { s.MenuId, s.PacienteId, s.FechaServicio });
        builder.Ignore(s => s.Id);

        builder.Property(s => s.MenuId).HasColumnName("menu_id");
        builder.Property(s => s.PacienteId).HasColumnName("paciente_id");

        builder.Property(s => s.FechaServicio)
            .HasColumnName("fecha_servicio")
            .HasColumnType("timestamp with time zone");

        // FK compuesta (menu_id, paciente_id) → Asignacion se configura desde AsignacionConfiguration (colección Servicios)

        // FK compuesta (menu_id, paciente_id, fecha_servicio) desde Consumo; no se elimina un servicio con consumos
        // (R7-14, que ahora pasa por el servicio)
        builder.HasMany(s => s.Consumos)
            .WithOne()
            .HasForeignKey(c => new { c.MenuId, c.PacienteId, c.FechaServicio })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
