using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class ConsumoConfiguration : IEntityTypeConfiguration<Consumo>
{
    public void Configure(EntityTypeBuilder<Consumo> builder)
    {
        // Consume(Servicio, ItemAlimentario): un consumo por ítem en cada servicio. Que el ítem esté en el menú
        // (R11-01) lo comprueba Persistence/Sql/R11_01_consumo_del_menu.sql.
        builder.ToTable("consumo");

        builder.HasKey(c => new { c.MenuId, c.PacienteId, c.FechaServicio, c.ItemId });
        builder.Ignore(c => c.Id);

        builder.Property(c => c.MenuId).HasColumnName("menu_id");
        builder.Property(c => c.PacienteId).HasColumnName("paciente_id");

        builder.Property(c => c.FechaServicio)
            .HasColumnName("fecha_servicio")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.ItemId).HasColumnName("item_id");

        builder.Property(c => c.Aceptacion)
            .HasColumnName("aceptacion")
            .IsRequired();

        // FK compuesta → Servicio se configura desde ServicioConfiguration (colección Consumos)
        builder.HasOne<ItemAlimentario>()
            .WithMany()
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
