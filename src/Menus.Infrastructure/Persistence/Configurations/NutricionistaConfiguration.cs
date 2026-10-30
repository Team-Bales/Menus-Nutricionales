using Menus.Domain.Entidades;
using Menus.Domain.Nomenclators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class NutricionistaConfiguration : IEntityTypeConfiguration<Nutricionista>
{
    public void Configure(EntityTypeBuilder<Nutricionista> builder)
    {
        // RD-07: nombre no vacío
        builder.ToTable("nutricionistas", t =>
        {
            t.HasCheckConstraint("ck_nutricionistas_r6_15_nombre", "btrim(nombre) <> ''");
        });

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(n => n.EspecialidadId).HasColumnName("especialidad_id");

        // RD-18: booleano obligatorio, falso por defecto. Que el revisor sea jefe (RS-03) lo comprueba
        // Persistence/Sql/RS_03_revisor_jefe.sql al registrar el dictamen.
        builder.Property(n => n.EsJefe)
            .HasColumnName("es_jefe")
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne<Especialidad>()
            .WithMany()
            .HasForeignKey(n => n.EspecialidadId)
            .OnDelete(DeleteBehavior.Restrict);

        // Autoriza: Nutricionista → NutricionistasDieta (Restrict: no se elimina un nutricionista con autorizaciones, RX-02)
        builder.HasMany(n => n.DietasAutorizadas)
            .WithOne()
            .HasForeignKey(nd => nd.NutricionistaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
