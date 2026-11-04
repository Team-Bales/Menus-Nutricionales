using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class SalaConfiguration : IEntityTypeConfiguration<Sala>
{
    public void Configure(EntityTypeBuilder<Sala> builder)
    {
        // RD-07: nombre no vacío
        builder.ToTable("salas", t =>
        {
            t.HasCheckConstraint("ck_salas_r6_15_nombre", "btrim(nombre) <> ''");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        // Sin UNIQUE: el nombre único solo se exige en los nomencladores (RD-08; #12, §6.1)
    }
}
