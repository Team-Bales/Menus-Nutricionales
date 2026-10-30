using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ConsumoConfiguration : IEntityTypeConfiguration<Consumo>
{
    public void Configure(EntityTypeBuilder<Consumo> builder)
    {
        // Consume(Servicio, ItemAlimentario): un consumo por ítem en cada servicio.
        builder.ToTable("consumo");

        builder.HasKey(c => new { c.MenuId, c.PacienteId, c.FechaServicio, c.ItemId });
        builder.Ignore(c => c.Id);

        builder.Property(c => c.MenuId).HasColumnName("menu_id");
        builder.Property(c => c.PacienteId).HasColumnName("paciente_id");

        builder.Property(c => c.FechaServicio)
            .HasColumnName("fecha_servicio")
            .HasColumnType("date");

        builder.Property(c => c.ItemId).HasColumnName("item_id");

        builder.Property(c => c.Aceptacion)
            .HasColumnName("aceptacion")
            .IsRequired();

        // FK compuesta → Servicio se configura desde ServicioConfiguration (colección Consumos)

        // RX-01: solo se consume lo que el menú incluye. Es la dependencia de inclusión consumo[menú, ítem] ⊆
        // menu_item[menú, ítem], así que va como FK compuesta (RESTRICT): tampoco se puede quitar del menú un ítem
        // que ya tiene consumos. La FK hacia el ítem queda implicada, porque menu_item.item_id ya la tiene.
        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(c => new { c.MenuId, c.ItemId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
