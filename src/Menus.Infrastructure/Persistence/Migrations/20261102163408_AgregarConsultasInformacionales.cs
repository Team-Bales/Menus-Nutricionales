using Menus.Infrastructure.Persistence.Consultas;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConsultasInformacionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El modelo no cambia: la migración crea las funciones de las consultas 2 a 6 del informe (§2.2).
            // La consulta 1 la agrega su propio issue (#102).
            migrationBuilder.AplicarConsulta("Consulta_2_alimentos_mas_usados.sql");
            migrationBuilder.AplicarConsulta("Consulta_3_dictamenes_de_un_revisor.sql");
            migrationBuilder.AplicarConsulta("Consulta_4_desempeno_ante_un_menu.sql");
            migrationBuilder.AplicarConsulta("Consulta_5_comparacion_entre_dietas.sql");
            migrationBuilder.AplicarConsulta("Consulta_6_nivel_calorico_y_resultado.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS consulta_2_alimentos_mas_usados(uuid, integer);
                DROP FUNCTION IF EXISTS consulta_3_dictamenes_de_un_revisor(uuid);
                DROP FUNCTION IF EXISTS consulta_4_desempeno_ante_un_menu(uuid);
                DROP FUNCTION IF EXISTS consulta_5_distribucion_por_grupo(uuid[], boolean);
                DROP FUNCTION IF EXISTS consulta_5_distribucion_por_nivel(uuid[], boolean);
                DROP FUNCTION IF EXISTS consulta_5_criterios_de_equilibrio(uuid[], boolean);
                DROP FUNCTION IF EXISTS consulta_6a_correlacion_por_dieta(timestamptz, timestamptz);
                DROP FUNCTION IF EXISTS consulta_6b_items_mas_rechazados(integer, integer);
                DROP FUNCTION IF EXISTS consulta_6c_revalorizados_frente_a_su_sala();
                """);
        }
    }
}
