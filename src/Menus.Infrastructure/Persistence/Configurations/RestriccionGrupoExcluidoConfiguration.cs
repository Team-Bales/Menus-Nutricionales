using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapea <see cref="RestriccionGrupoExcluido"/> (relación <c>Excluye</c>) a la tabla
/// <c>excluye</c>: los grupos nutricionales que excluye cada restricción alimentaria.
/// </summary>
internal sealed class RestriccionGrupoExcluidoConfiguration : IEntityTypeConfiguration<RestriccionGrupoExcluido>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RestriccionGrupoExcluido> builder)
    {
        builder.ToTable("excluye");

        // El par restricción y grupo identifica cada exclusión (RC-16)
        builder.HasKey(e => new { e.RestriccionAlimentariaId, e.GrupoNutricionalId });
        builder.Ignore(e => e.Id);

        builder.Property(e => e.RestriccionAlimentariaId).HasColumnName("restriccion_alimentaria_id");
        builder.Property(e => e.GrupoNutricionalId).HasColumnName("grupo_nutricional_id");

        // FK → RestriccionAlimentaria se configura desde RestriccionAlimentariaConfiguration (colección GruposExcluidos)
        // Restrict: no se elimina un grupo mientras alguna restricción lo excluya (RR-21)
        builder.HasOne<GrupoNutricional>()
            .WithMany()
            .HasForeignKey(e => e.GrupoNutricionalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
