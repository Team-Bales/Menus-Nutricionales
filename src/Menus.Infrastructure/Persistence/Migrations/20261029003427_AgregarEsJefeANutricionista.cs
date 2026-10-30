using Menus.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsJefeANutricionista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_jefe",
                table: "nutricionistas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // RS-03: quien registra un dictamen tiene que ser jefe. El trigger lee la columna recién creada, por
            // eso se aplica aquí y no en InitialCreate (RestriccionesNoDeclarativas.ScriptsPosteriores).
            migrationBuilder.AplicarScript("RS_03_revisor_jefe.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Primero el trigger, que lee la columna.
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_rs_03() CASCADE;");

            migrationBuilder.DropColumn(
                name: "es_jefe",
                table: "nutricionistas");
        }
    }
}
