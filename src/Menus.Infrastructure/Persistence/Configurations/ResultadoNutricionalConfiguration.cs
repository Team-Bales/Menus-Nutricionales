using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ResultadoNutricionalConfiguration : IEntityTypeConfiguration<ResultadoNutricional>
{
    public void Configure(EntityTypeBuilder<ResultadoNutricional> builder)
    {
        // RD-14: catálogo cerrado con exactamente estas cinco categorías y sus valores
        builder.ToTable("resultados_nutricionales", t =>
        {
            t.HasCheckConstraint("ck_resultados_nutricionales_r7_19_nombre", "nombre IN ('Muy mala', 'Mala', 'Normal', 'Buena', 'Excelente')");
            t.HasCheckConstraint("ck_resultados_nutricionales_r7_19_valor", "valor BETWEEN 1 AND 5");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Valor)
            .HasColumnName("valor")
            .IsRequired();

        builder.HasIndex(r => r.Nombre).IsUnique();
        builder.HasIndex(r => r.Valor).IsUnique();

        builder.HasData(ResultadoNutricional.Todos);
    }
}
