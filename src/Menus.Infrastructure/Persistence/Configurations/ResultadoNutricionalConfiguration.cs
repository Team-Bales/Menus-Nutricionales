using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ResultadoNutricionalConfiguration : IEntityTypeConfiguration<ResultadoNutricional>
{
    public void Configure(EntityTypeBuilder<ResultadoNutricional> builder)
    {
        builder.ToTable("resultados_nutricionales");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Valor)
            .HasColumnName("valor")
            .IsRequired();

        // Nombre y Valor únicos (R7-19); las cinco filas las siembra el #58 con ResultadoNutricional.Todos
        builder.HasIndex(r => r.Nombre).IsUnique();
        builder.HasIndex(r => r.Valor).IsUnique();
    }
}
