using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuariosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_email_no_vacio", "btrim(email) <> ''");
                    table.CheckConstraint("ck_usuarios_entidad_id_por_rol", "(rol = 'Admin' AND entidad_id IS NULL) OR (rol IN ('Nutricionista', 'Paciente') AND entidad_id IS NOT NULL)");
                    table.CheckConstraint("ck_usuarios_password_hash_no_vacio", "btrim(password_hash) <> ''");
                    table.CheckConstraint("ck_usuarios_rol_valido", "rol IN ('Admin', 'Nutricionista', 'Paciente')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_entidad_id_non_null",
                table: "usuarios",
                column: "entidad_id",
                unique: true,
                filter: "entidad_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
