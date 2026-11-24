using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="Dieta"/> a la tabla <c>dietas</c>, con su programa de atención. Los grupos que cubre y las
/// restricciones que aplica se borran con ella (RX-02).
/// </summary>
internal sealed class DietaConfiguration : IEntityTypeConfiguration<Dieta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Dieta> builder)
    {
        // RD-07: nombre no vacío
        builder.ToTable("dietas", t =>
        {
            t.HasCheckConstraint("ck_dietas_r6_15_nombre", "btrim(nombre) <> ''");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.ProgramaAtencionId).HasColumnName("programa_atencion_id");

        builder.HasOne<ProgramaAtencion>()
            .WithMany()
            .HasForeignKey(d => d.ProgramaAtencionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cascade: Dieta → CubreDieta y Restringe (RX-02)
        builder.HasMany(d => d.GruposNutricionales)
            .WithOne()
            .HasForeignKey(dg => dg.DietaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Restricciones)
            .WithOne()
            .HasForeignKey(dr => dr.DietaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
