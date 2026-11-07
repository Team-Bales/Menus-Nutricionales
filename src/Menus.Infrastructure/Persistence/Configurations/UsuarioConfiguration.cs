using Menus.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menus.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", t =>
        {
            // RD: Email no vacío (ya lo garantiza la normalización, pero CHECK por seguridad)
            t.HasCheckConstraint("ck_usuarios_email_no_vacio", "btrim(email) <> ''");
            // RD: PasswordHash no vacío
            t.HasCheckConstraint("ck_usuarios_password_hash_no_vacio", "btrim(password_hash) <> ''");
            // RD: Rol válido
            t.HasCheckConstraint("ck_usuarios_rol_valido", "rol IN ('Admin', 'Nutricionista', 'Paciente')");
            // RD: Admin => EntidadId IS NULL; Nutricionista/Paciente => EntidadId IS NOT NULL
            t.HasCheckConstraint("ck_usuarios_entidad_id_por_rol",
                "(rol = 'Admin' AND entidad_id IS NULL) OR (rol IN ('Nutricionista', 'Paciente') AND entidad_id IS NOT NULL)");
        });

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254) // RFC 5321
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(u => u.Rol)
            .HasColumnName("rol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.EntidadId)
            .HasColumnName("entidad_id");

        // Índice único simple sobre email (ya normalizado lowercase/trim)
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("ux_usuarios_email");

        // Índice único filtrado sobre EntidadId (solo non-null)
        builder.HasIndex(u => u.EntidadId)
            .IsUnique()
            .HasFilter("entidad_id IS NOT NULL")
            .HasDatabaseName("ux_usuarios_entidad_id_non_null");
    }
}
