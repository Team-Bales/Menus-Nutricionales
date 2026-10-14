using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class NutricionistaConfiguration : IEntityTypeConfiguration<Nutricionista>
{
    public void Configure(EntityTypeBuilder<Nutricionista> builder)
    {
        builder.ToTable("nutricionistas");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(n => n.EspecialidadId).HasColumnName("especialidad_id");

        builder.HasOne<Especialidad>()
            .WithMany()
            .HasForeignKey(n => n.EspecialidadId)
            .OnDelete(DeleteBehavior.Restrict);

        // Autoriza: Nutricionista → NutricionistasDieta (Cascade: si se elimina el nutricionista, se eliminan sus autorizaciones)
        builder.HasMany(n => n.DietasAutorizadas)
            .WithOne()
            .HasForeignKey(nd => nd.NutricionistaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
