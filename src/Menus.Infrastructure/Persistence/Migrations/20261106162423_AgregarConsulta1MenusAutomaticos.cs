using Menus.Infrastructure.Persistence.Consultas;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConsulta1MenusAutomaticos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El modelo no cambia: la migración crea la función de la consulta 1 del informe (§2.2), que el
            // #217 dejó pendiente para este issue (#102).
            migrationBuilder.AplicarConsulta("Consulta_1_menus_automaticos_por_dieta.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS consulta_1_menus_automaticos_por_dieta(uuid);");
        }
    }
}
