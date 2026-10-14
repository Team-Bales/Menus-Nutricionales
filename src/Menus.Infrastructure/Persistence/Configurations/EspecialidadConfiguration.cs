using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class EspecialidadConfiguration : IEntityTypeConfiguration<Especialidad>
{
    public void Configure(EntityTypeBuilder<Especialidad> builder)
    {
        builder.ToTable("especialidades");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.Nombre).IsUnique();

        builder.HasData(
            new Especialidad(new Guid("44444444-0000-0000-0000-000000000001"), "Nutrición Clínica"),
            new Especialidad(new Guid("44444444-0000-0000-0000-000000000002"), "Dietética Hospitalaria"),
            new Especialidad(new Guid("44444444-0000-0000-0000-000000000003"), "Nutrición Deportiva"));
    }
}
