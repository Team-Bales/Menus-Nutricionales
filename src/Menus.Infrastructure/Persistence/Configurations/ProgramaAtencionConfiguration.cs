using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ProgramaAtencionConfiguration : IEntityTypeConfiguration<ProgramaAtencion>
{
    public void Configure(EntityTypeBuilder<ProgramaAtencion> builder)
    {
        builder.ToTable("programas_atencion");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Nombre).IsUnique();
    }
}
