using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ConsumoConfiguration : IEntityTypeConfiguration<Consumo>
{
    public void Configure(EntityTypeBuilder<Consumo> builder)
    {
        builder.ToTable("consumo");

        builder.HasKey(c => new { c.MenuId, c.PacienteId, c.AlimentoId });
        builder.Ignore(c => c.Id);

        builder.Property(c => c.MenuId).HasColumnName("menu_id");
        builder.Property(c => c.PacienteId).HasColumnName("paciente_id");
        builder.Property(c => c.AlimentoId).HasColumnName("alimento_id");

        builder.Property(c => c.Aceptacion)
            .HasColumnName("aceptacion")
            .IsRequired();

        // FK compuesta → Asignacion se configura desde AsignacionConfiguration (colección Consumos)
        builder.HasOne<Alimento>()
            .WithMany()
            .HasForeignKey(c => c.AlimentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
