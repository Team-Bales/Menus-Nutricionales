using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Menus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "especialidades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_especialidades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grupos_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_nutricionales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "niveles_caloricos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveles_caloricos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "programas_atencion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programas_atencion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restricciones_alimentarias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restricciones_alimentarias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resultados_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultados_nutricionales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "salas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_preparacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_preparacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nutricionistas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    especialidad_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutricionistas", x => x.id);
                    table.ForeignKey(
                        name: "FK_nutricionistas_especialidades_especialidad_id",
                        column: x => x.especialidad_id,
                        principalTable: "especialidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dietas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    programa_atencion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dietas", x => x.id);
                    table.ForeignKey(
                        name: "FK_dietas_programas_atencion_programa_atencion_id",
                        column: x => x.programa_atencion_id,
                        principalTable: "programas_atencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    edad = table.Column<int>(type: "integer", nullable: false),
                    sala_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                    table.ForeignKey(
                        name: "FK_pacientes_salas_sala_id",
                        column: x => x.sala_id,
                        principalTable: "salas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alimentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_preparacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nivel_calorico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alimentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_alimentos_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alimentos_niveles_caloricos_nivel_calorico_id",
                        column: x => x.nivel_calorico_id,
                        principalTable: "niveles_caloricos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alimentos_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alimentos_tipos_preparacion_tipo_preparacion_id",
                        column: x => x.tipo_preparacion_id,
                        principalTable: "tipos_preparacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_preparacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nivel_calorico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platos", x => x.id);
                    table.ForeignKey(
                        name: "FK_platos_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platos_niveles_caloricos_nivel_calorico_id",
                        column: x => x.nivel_calorico_id,
                        principalTable: "niveles_caloricos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platos_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platos_tipos_preparacion_tipo_preparacion_id",
                        column: x => x.tipo_preparacion_id,
                        principalTable: "tipos_preparacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "autoriza",
                columns: table => new
                {
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_autoriza", x => new { x.nutricionista_id, x.dieta_id });
                    table.ForeignKey(
                        name: "FK_autoriza_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_autoriza_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cubre_dieta",
                columns: table => new
                {
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cubre_dieta", x => new { x.dieta_id, x.grupo_nutricional_id });
                    table.ForeignKey(
                        name: "FK_cubre_dieta_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cubre_dieta_grupos_nutricionales_grupo_nutricional_id",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_creador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cantidad_total_alimentos = table.Column<int>(type: "integer", nullable: false),
                    es_automatico = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.id);
                    table.ForeignKey(
                        name: "FK_menus_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menus_nutricionistas_nutricionista_creador_id",
                        column: x => x.nutricionista_creador_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "restringe",
                columns: table => new
                {
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restriccion_alimentaria_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restringe", x => new { x.dieta_id, x.restriccion_alimentaria_id });
                    table.ForeignKey(
                        name: "FK_restringe_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_restringe_restricciones_alimentarias_restriccion_alimentari~",
                        column: x => x.restriccion_alimentaria_id,
                        principalTable: "restricciones_alimentarias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paciente_dieta",
                columns: table => new
                {
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dieta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente_dieta", x => new { x.paciente_id, x.dieta_id });
                    table.ForeignKey(
                        name: "FK_paciente_dieta_dietas_dieta_id",
                        column: x => x.dieta_id,
                        principalTable: "dietas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_paciente_dieta_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "composicion",
                columns: table => new
                {
                    plato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_composicion", x => new { x.plato_id, x.alimento_id });
                    table.ForeignKey(
                        name: "FK_composicion_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_composicion_platos_plato_id",
                        column: x => x.plato_id,
                        principalTable: "platos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asignacion",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asignacion", x => new { x.menu_id, x.paciente_id });
                    table.ForeignKey(
                        name: "FK_asignacion_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asignacion_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_alimento",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_alimento", x => new { x.menu_id, x.alimento_id });
                    table.ForeignKey(
                        name: "FK_menu_alimento_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menu_alimento_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_distribucion",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_preparacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proporcion = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_distribucion", x => new { x.menu_id, x.tipo_preparacion_id });
                    table.ForeignKey(
                        name: "FK_menu_distribucion_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_menu_distribucion_tipos_preparacion_tipo_preparacion_id",
                        column: x => x.tipo_preparacion_id,
                        principalTable: "tipos_preparacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_grupo_nutricional",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grupo_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_grupo_nutricional", x => new { x.menu_id, x.grupo_nutricional_id });
                    table.ForeignKey(
                        name: "FK_menu_grupo_nutricional_grupos_nutricionales_grupo_nutricion~",
                        column: x => x.grupo_nutricional_id,
                        principalTable: "grupos_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menu_grupo_nutricional_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_plato",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plato_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_plato", x => new { x.menu_id, x.plato_id });
                    table.ForeignKey(
                        name: "FK_menu_plato_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_menu_plato_platos_plato_id",
                        column: x => x.plato_id,
                        principalTable: "platos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "validacion",
                columns: table => new
                {
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_validacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validacion", x => new { x.nutricionista_id, x.menu_id });
                    table.ForeignKey(
                        name: "FK_validacion_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validacion_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consumo",
                columns: table => new
                {
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alimento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aceptacion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumo", x => new { x.menu_id, x.paciente_id, x.alimento_id });
                    table.ForeignKey(
                        name: "FK_consumo_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consumo_asignacion_menu_id_paciente_id",
                        columns: x => new { x.menu_id, x.paciente_id },
                        principalTable: "asignacion",
                        principalColumns: new[] { "menu_id", "paciente_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "valoraciones_nutricionales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resultado_nutricional_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_valoracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valoraciones_nutricionales", x => x.id);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_asignacion_menu_id_paciente_id",
                        columns: x => new { x.menu_id, x.paciente_id },
                        principalTable: "asignacion",
                        principalColumns: new[] { "menu_id", "paciente_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_valoraciones_nutricionales_resultados_nutricionales_resulta~",
                        column: x => x.resultado_nutricional_id,
                        principalTable: "resultados_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revalorizaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_solicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nutricionista_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valoracion_revisada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valoracion_generada_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revalorizaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_valoraciones_nutricionales_valoracion_gene~",
                        column: x => x.valoracion_generada_id,
                        principalTable: "valoraciones_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revalorizaciones_valoraciones_nutricionales_valoracion_revi~",
                        column: x => x.valoracion_revisada_id,
                        principalTable: "valoraciones_nutricionales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "niveles_caloricos",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("66666666-0000-0000-0000-000000000001"), "Bajo" },
                    { new Guid("66666666-0000-0000-0000-000000000002"), "Medio" },
                    { new Guid("66666666-0000-0000-0000-000000000003"), "Alto" }
                });

            migrationBuilder.InsertData(
                table: "tipos_preparacion",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("55555555-0000-0000-0000-000000000001"), "Entrante" },
                    { new Guid("55555555-0000-0000-0000-000000000002"), "Plato fuerte" },
                    { new Guid("55555555-0000-0000-0000-000000000003"), "Postre" },
                    { new Guid("55555555-0000-0000-0000-000000000004"), "Bebida" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_alimentos_grupo_nutricional_id",
                table: "alimentos",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_alimentos_nivel_calorico_id",
                table: "alimentos",
                column: "nivel_calorico_id");

            migrationBuilder.CreateIndex(
                name: "IX_alimentos_nombre",
                table: "alimentos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alimentos_nutricionista_id",
                table: "alimentos",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_alimentos_tipo_preparacion_id",
                table: "alimentos",
                column: "tipo_preparacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_asignacion_paciente_id",
                table: "asignacion",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_autoriza_dieta_id",
                table: "autoriza",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_composicion_alimento_id",
                table: "composicion",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "IX_consumo_alimento_id",
                table: "consumo",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "IX_cubre_dieta_grupo_nutricional_id",
                table: "cubre_dieta",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_dietas_programa_atencion_id",
                table: "dietas",
                column: "programa_atencion_id");

            migrationBuilder.CreateIndex(
                name: "IX_especialidades_nombre",
                table: "especialidades",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grupos_nutricionales_nombre",
                table: "grupos_nutricionales",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_alimento_alimento_id",
                table: "menu_alimento",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_distribucion_tipo_preparacion_id",
                table: "menu_distribucion",
                column: "tipo_preparacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_grupo_nutricional_grupo_nutricional_id",
                table: "menu_grupo_nutricional",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_plato_plato_id",
                table: "menu_plato",
                column: "plato_id");

            migrationBuilder.CreateIndex(
                name: "IX_menus_dieta_id",
                table: "menus",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_menus_nutricionista_creador_id",
                table: "menus",
                column: "nutricionista_creador_id");

            migrationBuilder.CreateIndex(
                name: "IX_niveles_caloricos_nombre",
                table: "niveles_caloricos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nutricionistas_especialidad_id",
                table: "nutricionistas",
                column: "especialidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_paciente_dieta_dieta_id",
                table: "paciente_dieta",
                column: "dieta_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_sala_id",
                table: "pacientes",
                column: "sala_id");

            migrationBuilder.CreateIndex(
                name: "IX_platos_grupo_nutricional_id",
                table: "platos",
                column: "grupo_nutricional_id");

            migrationBuilder.CreateIndex(
                name: "IX_platos_nivel_calorico_id",
                table: "platos",
                column: "nivel_calorico_id");

            migrationBuilder.CreateIndex(
                name: "IX_platos_nombre",
                table: "platos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platos_nutricionista_id",
                table: "platos",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_platos_tipo_preparacion_id",
                table: "platos",
                column: "tipo_preparacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_programas_atencion_nombre",
                table: "programas_atencion",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restricciones_alimentarias_nombre",
                table: "restricciones_alimentarias",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_restringe_restriccion_alimentaria_id",
                table: "restringe",
                column: "restriccion_alimentaria_id");

            migrationBuilder.CreateIndex(
                name: "IX_resultados_nutricionales_nombre",
                table: "resultados_nutricionales",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resultados_nutricionales_valor",
                table: "resultados_nutricionales",
                column: "valor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_nutricionista_id",
                table: "revalorizaciones",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_paciente_id",
                table: "revalorizaciones",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_valoracion_generada_id",
                table: "revalorizaciones",
                column: "valoracion_generada_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revalorizaciones_valoracion_revisada_id",
                table: "revalorizaciones",
                column: "valoracion_revisada_id");

            migrationBuilder.CreateIndex(
                name: "IX_salas_nombre",
                table: "salas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipos_preparacion_nombre",
                table: "tipos_preparacion",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_validacion_menu_id",
                table: "validacion",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_menu_id_paciente_id",
                table: "valoraciones_nutricionales",
                columns: new[] { "menu_id", "paciente_id" });

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_nutricionista_id",
                table: "valoraciones_nutricionales",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "IX_valoraciones_nutricionales_resultado_nutricional_id",
                table: "valoraciones_nutricionales",
                column: "resultado_nutricional_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "autoriza");

            migrationBuilder.DropTable(
                name: "composicion");

            migrationBuilder.DropTable(
                name: "consumo");

            migrationBuilder.DropTable(
                name: "cubre_dieta");

            migrationBuilder.DropTable(
                name: "menu_alimento");

            migrationBuilder.DropTable(
                name: "menu_distribucion");

            migrationBuilder.DropTable(
                name: "menu_grupo_nutricional");

            migrationBuilder.DropTable(
                name: "menu_plato");

            migrationBuilder.DropTable(
                name: "paciente_dieta");

            migrationBuilder.DropTable(
                name: "restringe");

            migrationBuilder.DropTable(
                name: "revalorizaciones");

            migrationBuilder.DropTable(
                name: "validacion");

            migrationBuilder.DropTable(
                name: "alimentos");

            migrationBuilder.DropTable(
                name: "platos");

            migrationBuilder.DropTable(
                name: "restricciones_alimentarias");

            migrationBuilder.DropTable(
                name: "valoraciones_nutricionales");

            migrationBuilder.DropTable(
                name: "grupos_nutricionales");

            migrationBuilder.DropTable(
                name: "niveles_caloricos");

            migrationBuilder.DropTable(
                name: "tipos_preparacion");

            migrationBuilder.DropTable(
                name: "asignacion");

            migrationBuilder.DropTable(
                name: "resultados_nutricionales");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "dietas");

            migrationBuilder.DropTable(
                name: "nutricionistas");

            migrationBuilder.DropTable(
                name: "salas");

            migrationBuilder.DropTable(
                name: "programas_atencion");

            migrationBuilder.DropTable(
                name: "especialidades");
        }
    }
}
