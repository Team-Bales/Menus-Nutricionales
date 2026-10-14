using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class NutricionistasDietaConfiguration : IEntityTypeConfiguration<NutricionistasDieta>
{
    public void Configure(EntityTypeBuilder<NutricionistasDieta> builder)
    {
        builder.ToTable("autoriza");

        builder.HasKey(nd => new { nd.NutricionistaId, nd.DietaId });
        builder.Ignore(nd => nd.Id);

        builder.Property(nd => nd.NutricionistaId).HasColumnName("nutricionista_id");
        builder.Property(nd => nd.DietaId).HasColumnName("dieta_id");

        builder.HasOne<Dieta>()
            .WithMany()
            .HasForeignKey(nd => nd.DietaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
