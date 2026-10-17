using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class RestriccionAlimentariaConfiguration : IEntityTypeConfiguration<RestriccionAlimentaria>
{
    public void Configure(EntityTypeBuilder<RestriccionAlimentaria> builder)
    {
        builder.ToTable("restricciones_alimentarias");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(r => r.Nombre).IsUnique();

        // Excluye: Restrict, no se elimina una restricción mientras excluya algún grupo (R6-08b)
        builder.HasMany(r => r.GruposExcluidos)
            .WithOne()
            .HasForeignKey(e => e.RestriccionAlimentariaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new RestriccionAlimentaria(new Guid("22222222-0000-0000-0000-000000000001"), "Sin gluten"),
            new RestriccionAlimentaria(new Guid("22222222-0000-0000-0000-000000000002"), "Hiposódica"),
            new RestriccionAlimentaria(new Guid("22222222-0000-0000-0000-000000000003"), "Sin lactosa"),
            new RestriccionAlimentaria(new Guid("22222222-0000-0000-0000-000000000004"), "Vegetariana"),
            new RestriccionAlimentaria(new Guid("22222222-0000-0000-0000-000000000005"), "Vegana"));
    }
}
