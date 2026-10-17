using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ProgramaAtencionConfiguration : IEntityTypeConfiguration<ProgramaAtencion>
{
    public void Configure(EntityTypeBuilder<ProgramaAtencion> builder)
    {
        // R6-15: nombre no vacío
        builder.ToTable("programas_atencion", t =>
        {
            t.HasCheckConstraint("ck_programas_atencion_r6_15_nombre", "btrim(nombre) <> ''");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => p.Nombre).IsUnique();

        builder.HasData(
            new ProgramaAtencion(new Guid("33333333-0000-0000-0000-000000000001"), "Nutrición Pediátrica"),
            new ProgramaAtencion(new Guid("33333333-0000-0000-0000-000000000002"), "Oncología"),
            new ProgramaAtencion(new Guid("33333333-0000-0000-0000-000000000003"), "Geriatría"));
    }
}
