using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ValoracionNutricionalConfiguration : IEntityTypeConfiguration<ValoracionNutricional>
{
    public void Configure(EntityTypeBuilder<ValoracionNutricional> builder)
    {
        builder.ToTable("valoraciones_nutricionales");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.MenuId).HasColumnName("menu_id");
        builder.Property(v => v.PacienteId).HasColumnName("paciente_id");
        builder.Property(v => v.NutricionistaId).HasColumnName("nutricionista_id");
        builder.Property(v => v.ResultadoNutricionalId).HasColumnName("resultado_nutricional_id");

        // Evalúa: FK compuesta → Asignacion (R7-12); no se borra una asignación valorada (R7-14)
        builder.HasOne<Asignacion>()
            .WithMany()
            .HasForeignKey(v => new { v.MenuId, v.PacienteId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Nutricionista>()
            .WithMany()
            .HasForeignKey(v => v.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ResultadoNutricional>()
            .WithMany()
            .HasForeignKey(v => v.ResultadoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
