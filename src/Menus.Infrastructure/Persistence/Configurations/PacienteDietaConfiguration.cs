using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class PacienteDietaConfiguration : IEntityTypeConfiguration<PacienteDieta>
{
    public void Configure(EntityTypeBuilder<PacienteDieta> builder)
    {
        builder.ToTable("paciente_dieta");

        builder.HasKey(pd => new { pd.PacienteId, pd.DietaId });
        builder.Ignore(pd => pd.Id);

        builder.Property(pd => pd.PacienteId).HasColumnName("paciente_id");
        builder.Property(pd => pd.DietaId).HasColumnName("dieta_id");

        // FK → Dieta se configura cuando esa entidad se incorpore (issue #51)
    }
}
