using Menus.Infrastructure.Persistence.Sql;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CerrarParametrizacionEItemsDelMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El modelo no cambia: la migración solo agrega dos restricciones que la base no cubría del todo.
            // RS-07: la parametrización de un menú no se amplía después de creado.
            migrationBuilder.AplicarScript("RS_07_parametrizacion_sin_altas.sql");

            // RS-15: los ítems de un menú no cambian después de su dictamen.
            migrationBuilder.AplicarScript("RS_15_items_de_menu_dictaminado.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS fn_rs_15_items() CASCADE;
                DROP FUNCTION IF EXISTS fn_rs_15_exigir_menu_abierto(uuid);
                DROP FUNCTION IF EXISTS fn_rs_15_dictamen_registrado() CASCADE;
                DROP FUNCTION IF EXISTS fn_rs_07_alta_parametro() CASCADE;
                DROP FUNCTION IF EXISTS fn_rs_07_menu_creado() CASCADE;
                """);
        }
    }
}
